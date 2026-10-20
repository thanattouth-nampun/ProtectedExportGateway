using Gateway.Application;
using Gateway.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Infrastructure.Persistence;

public sealed class SqlOwnerWorkspace(DbContextOptions<GatewayDbContext> options,PortalIdentityDirectory directory):IOwnerWorkspace
{
    private void Authorize(OwnerActor actor)
    {
        if (!actor.IsWorkspaceMember) throw new WorkspaceFault(403,"INTERNAL_MEMBER_REQUIRED");
        if (actor.Principal.TenantId!=directory.TenantId || actor.Principal.ObjectId==Guid.Empty)
            throw new WorkspaceFault(403,"TENANT_OR_IDENTITY_INVALID");
    }
    private Guid[] Validate(OwnerActor actor,DraftEdit edit)
    {
        Authorize(actor);
        if (edit.Title is null || string.IsNullOrWhiteSpace(edit.Title) || edit.Title.Length>200
            || edit.Purpose is null || string.IsNullOrWhiteSpace(edit.Purpose) || edit.Purpose.Length>1000
            || edit.RequesterObjectIds is null || edit.RequesterObjectIds.Length>50
            || edit.RequesterObjectIds.Distinct().Count()!=edit.RequesterObjectIds.Length
            || edit.RequesterObjectIds.Any(id=>!directory.Contains(new(actor.Principal.TenantId,id))))
            throw new WorkspaceFault(400,"DRAFT_REQUEST_INVALID");
        return edit.RequesterObjectIds.Order().ToArray();
    }
    private static Task<DocumentRow?> Owned(GatewayDbContext db,OwnerActor actor,Guid id,CancellationToken ct)=>
        db.Documents.SingleOrDefaultAsync(d=>d.Id==id && d.TenantId==actor.Principal.TenantId && d.OwnerOid==actor.Principal.ObjectId,ct);
    private static async Task<OwnerDocumentView> View(GatewayDbContext db,DocumentRow row,CancellationToken ct)
    {
        var acl=await db.DocumentAcls.AsNoTracking().Where(x=>x.DocumentId==row.Id && x.TenantId==row.TenantId && x.CanRequestProtectedCopy)
            .Select(x=>x.RequesterOid).OrderBy(x=>x).ToArrayAsync(ct);
        var hasSource=await db.DocumentVersions.AnyAsync(x=>x.DocumentId==row.Id && x.TenantId==row.TenantId,ct);
        return new(row.Id,row.Title,row.Purpose,row.Revision,row.AclRevision,'"'+Convert.ToBase64String(row.RowVersion)+'"',acl,
            row.Status.ToString(),hasSource?"VerificationRequired":"NoSourceUploaded",false);
    }
    public async Task<OwnerDocumentPage> List(OwnerActor actor,int skip,CancellationToken ct=default)
    {
        Authorize(actor);
        if (skip is <0 or >10000) throw new WorkspaceFault(400,"PAGE_INVALID");
        await using var db=new GatewayDbContext(options);
        var query=db.Documents.AsNoTracking().Where(x=>x.TenantId==actor.Principal.TenantId && x.OwnerOid==actor.Principal.ObjectId);
        var total=await query.CountAsync(ct);
        var rows=await query.OrderBy(x=>x.Title).ThenBy(x=>x.Id).Skip(skip).Take(25).ToListAsync(ct);
        var views=new List<OwnerDocumentView>();
        foreach (var row in rows) views.Add(await View(db,row,ct));
        return new(views.ToArray(),total,skip,25);
    }
    public async Task<OwnerDocumentView> Find(OwnerActor actor,Guid id,CancellationToken ct=default)
    {
        Authorize(actor); await using var db=new GatewayDbContext(options);
        var row=await Owned(db,actor,id,ct) ?? throw new WorkspaceFault(404,"DOCUMENT_NOT_FOUND");
        return await View(db,row,ct);
    }
    public async Task<OwnerDocumentView> Create(OwnerActor actor,DraftEdit edit,Guid correlation,CancellationToken ct=default)
    {
        var ids=Validate(actor,edit);
        await using var db=new GatewayDbContext(options);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var row=new DocumentRow {Id=Guid.NewGuid(),TenantId=actor.Principal.TenantId,OwnerOid=actor.Principal.ObjectId,
            Title=edit.Title.Trim(),Purpose=edit.Purpose.Trim(),Status=PublicationStatus.Draft};
        db.Documents.Add(row);
        foreach (var id in ids) db.DocumentAcls.Add(new() {DocumentId=row.Id,TenantId=row.TenantId,RequesterOid=id,CanRequestProtectedCopy=true});
        db.AuditEvents.Add(Event(row,actor,"DocumentDraftCreated",correlation));
        await db.SaveChangesAsync(ct);
        var result=await View(db,row,ct); await tx.CommitAsync(ct);
        return result;
    }
    public async Task<OwnerDocumentView> Update(OwnerActor actor,Guid id,string etag,DraftEdit edit,Guid correlation,CancellationToken ct=default)
    {
        var ids=Validate(actor,edit);
        await using var db=new GatewayDbContext(options);
        await using var tx=await db.Database.BeginTransactionAsync(ct);
        var row=await db.Documents.FromSqlInterpolated($"SELECT * FROM [Documents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={id} AND [TenantId]={actor.Principal.TenantId} AND [OwnerOid]={actor.Principal.ObjectId}")
            .SingleOrDefaultAsync(ct) ?? throw new WorkspaceFault(404,"DOCUMENT_NOT_FOUND");
        if (string.IsNullOrEmpty(etag)) throw new WorkspaceFault(428,"IF_MATCH_REQUIRED");
        if (etag!='"'+Convert.ToBase64String(row.RowVersion)+'"') throw new WorkspaceFault(412,"REVISION_CONFLICT");
        // Until protected intake proves the RMS owner binding, only empty private drafts may be mutated.
        if (row.Status!=PublicationStatus.Draft || row.CurrentPublishedVersionId is not null
            || await db.DocumentVersions.AnyAsync(x=>x.DocumentId==id && x.TenantId==row.TenantId,ct))
            throw new WorkspaceFault(409,"SOURCE_OWNER_VERIFICATION_REQUIRED");
        var oldAcl=await db.DocumentAcls.Where(x=>x.DocumentId==id && x.TenantId==row.TenantId).ToListAsync(ct);
        var aclChanged=!oldAcl.Where(x=>x.CanRequestProtectedCopy).Select(x=>x.RequesterOid).ToHashSet().SetEquals(ids);
        foreach (var entry in oldAcl) db.DocumentAcls.Remove(entry);
        // Existing keyed rows are updated/reused to avoid EF tracking two objects with the same key.
        foreach (var requester in ids)
        {
            var existing=oldAcl.SingleOrDefault(x=>x.RequesterOid==requester);
            if (existing is not null) { db.Entry(existing).State=EntityState.Unchanged; existing.CanRequestProtectedCopy=true; }
            else db.DocumentAcls.Add(new() {DocumentId=id,TenantId=row.TenantId,RequesterOid=requester,CanRequestProtectedCopy=true});
        }
        row.Title=edit.Title.Trim(); row.Purpose=edit.Purpose.Trim(); row.Revision++;
        if (aclChanged) row.AclRevision++;
        db.AuditEvents.Add(Event(row,actor,aclChanged?"DocumentDraftAclUpdated":"DocumentDraftMetadataUpdated",correlation));
        OwnerDocumentView result;
        try { await db.SaveChangesAsync(ct); result=await View(db,row,ct); await tx.CommitAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new WorkspaceFault(412,"REVISION_CONFLICT"); }
        return result;
    }
    public async Task<OwnerAuditView[]> Audit(OwnerActor actor,Guid id,CancellationToken ct=default)
    {
        Authorize(actor); await using var db=new GatewayDbContext(options);
        _=await Owned(db,actor,id,ct) ?? throw new WorkspaceFault(404,"DOCUMENT_NOT_FOUND");
        return await db.AuditEvents.AsNoTracking().Where(x=>x.DocumentId==id && x.TenantId==actor.Principal.TenantId)
            .OrderByDescending(x=>x.At).ThenByDescending(x=>x.DocumentRevision).Take(50)
            .Select(x=>new OwnerAuditView(x.EventType,x.DocumentRevision,x.At,x.CorrelationId)).ToArrayAsync(ct);
    }
    private static AuditEventRow Event(DocumentRow row,OwnerActor actor,string kind,Guid correlation)=>new()
    {Id=Guid.NewGuid(),TenantId=row.TenantId,ActorOid=actor.Principal.ObjectId,DocumentId=row.Id,DocumentRevision=row.Revision,
        EventType=kind,At=DateTimeOffset.UtcNow,CorrelationId=correlation,Mode=ExecutionMode.Simulated};
}
