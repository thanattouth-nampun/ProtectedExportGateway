using Gateway.Application;
using Gateway.Domain;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace Gateway.Infrastructure.Persistence;

// Guarded local intake only: never inspects/decrypts/parses, promotes, marks Eligible or publishes.
public sealed class SqlQuarantineIntake(DbContextOptions<GatewayDbContext> options,PortalIdentityDirectory directory,
    IQuarantineObjects objects,TimeProvider clock):IQuarantineIntake
{
    private DateTimeOffset Now=>clock.GetUtcNow();
    private void Authorize(OwnerActor actor)
    {
        if(!actor.IsWorkspaceMember || actor.Principal.TenantId!=directory.TenantId || actor.Principal.ObjectId==Guid.Empty)
            throw new WorkspaceFault(403,"INTERNAL_MEMBER_REQUIRED");
    }
    private static async Task<DocumentRow> Owned(GatewayDbContext db,OwnerActor actor,Guid document,CancellationToken ct)=>
        await db.Documents.FromSqlInterpolated($"SELECT * FROM [Documents] WITH (UPDLOCK,HOLDLOCK) WHERE [Id]={document} AND [TenantId]={actor.Principal.TenantId} AND [OwnerOid]={actor.Principal.ObjectId}")
            .SingleOrDefaultAsync(ct) ?? throw new WorkspaceFault(404,"DOCUMENT_NOT_FOUND");
    private static QuarantineVersionView View(DocumentVersionRow row)=>new(row.Id,row.DisplayFilename,row.State.ToString(),row.QuarantineBytes,row.QuarantineSha256,row.UploadedAt,row.UploadErrorCode);
    private void Audit(GatewayDbContext db,DocumentRow document,Guid actor,string kind)
    {
        document.Revision++;
        db.AuditEvents.Add(new(){Id=Guid.NewGuid(),TenantId=document.TenantId,DocumentId=document.Id,DocumentRevision=document.Revision,
            ActorOid=actor,EventType=kind,At=Now,CorrelationId=Guid.NewGuid(),Mode=ExecutionMode.Simulated});
    }
    private async Task<(DocumentVersionRow Row,bool Replay)> Reserve(OwnerActor actor,Guid document,string etag,Guid key,string filename,CancellationToken ct)
    {
        await using var db=new GatewayDbContext(options);await using var tx=await db.Database.BeginTransactionAsync(ct);
        // Serialize bounded queue reservations across documents/processes; lock belongs to this transaction.
        await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource=N'LocalQuarantineReserve-v1',@LockMode=N'Exclusive',@LockOwner=N'Transaction',@LockTimeout=5000; IF @r<0 THROW 51001,'QUARANTINE_BUSY',1;",ct);
        var doc=await Owned(db,actor,document,ct);
        if(!doc.Enabled || doc.Status!=PublicationStatus.Draft || doc.CurrentPublishedVersionId is not null)
            throw new WorkspaceFault(409,"SOURCE_OWNER_VERIFICATION_REQUIRED");
        if(await db.DocumentVersions.AnyAsync(x=>x.DocumentId==document && (x.UploadKey==null || x.SourceBindingJson!=null),ct))
            throw new WorkspaceFault(409,"SOURCE_OWNER_VERIFICATION_REQUIRED");
        var row=await db.DocumentVersions.SingleOrDefaultAsync(x=>x.DocumentId==document && x.TenantId==doc.TenantId && x.UploaderOid==actor.Principal.ObjectId && x.UploadKey==key,ct);
        if(row is not null)
        {
            if(row.DisplayFilename!=filename) throw new WorkspaceFault(409,"UPLOAD_KEY_CONFLICT");
            if(row.State==IntakeState.AwaitingValidation) {await tx.CommitAsync(ct);return(row,true);}
            if(row.State==IntakeState.Uploading) throw new WorkspaceFault(409,"UPLOAD_IN_PROGRESS");
            if(row.State!=IntakeState.ValidationFailed || row.UploadAttempts>=3 || row.UploadErrorCode is not ("UPLOAD_INTERRUPTED" or "UPLOAD_FAILED" or "UPLOAD_ABANDONED"))
                throw new WorkspaceFault(409,"UPLOAD_KEY_TERMINAL");
        }
        if(string.IsNullOrEmpty(etag)) throw new WorkspaceFault(428,"IF_MATCH_REQUIRED");
        if(etag!='"'+Convert.ToBase64String(doc.RowVersion)+'"') throw new WorkspaceFault(412,"REVISION_CONFLICT");
        var retryId=row?.Id ?? Guid.Empty;var recent=Now.AddHours(-24);
        if(await db.DocumentVersions.CountAsync(x=>x.Id!=retryId && x.UploadKey!=null && (x.State==IntakeState.Uploading || x.State==IntakeState.AwaitingValidation || x.UploadedAt>=recent),ct)>=20)
            throw new WorkspaceFault(429,"QUARANTINE_QUEUE_FULL");
        var retry=row is not null;
        row ??=new(){Id=Guid.NewGuid(),DocumentId=document,TenantId=doc.TenantId,UploaderOid=actor.Principal.ObjectId,UploadKey=key,DisplayFilename=filename};
        if(!retry) db.DocumentVersions.Add(row);
        row.QuarantineToken=Guid.NewGuid();row.QuarantineBytes=null;row.QuarantineSha256=null;row.UploadErrorCode=null;
        row.State=IntakeState.Uploading;row.UploadAttempts++;row.UploadedAt=Now;row.UploadLeaseUntil=Now.AddMinutes(5);
        Audit(db,doc,actor.Principal.ObjectId,retry?"QuarantineUploadRetried":"QuarantineUploadStarted");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return(row,false);
    }
    public async Task<QuarantineVersionView> Upload(OwnerActor actor,Guid document,string etag,Guid key,string filename,bool synthetic,Stream input,CancellationToken ct=default)
    {
        QuarantineLimits.Validate(actor,directory.TenantId,key,filename,synthetic);
        var (reserved,replay)=await Reserve(actor,document,etag,key,filename,ct);
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromMinutes(2));
        if(replay)
        {
            // Re-measure every retry's actual bytes; a claimed checksum is never an authority.
            long length=0;using var hash=IncrementalHash.CreateHash(HashAlgorithmName.SHA256);var buffer=new byte[65536];int n;
            while((n=await input.ReadAsync(buffer.AsMemory(0,(int)Math.Min(buffer.Length,QuarantineLimits.MaxBytes-length+1)),timeout.Token))!=0)
            {length+=n;if(length>QuarantineLimits.MaxBytes) throw new WorkspaceFault(413,"UPLOAD_TOO_LARGE");hash.AppendData(buffer,0,n);}
            if(length!=reserved.QuarantineBytes || Convert.ToHexString(hash.GetHashAndReset())!=reserved.QuarantineSha256)
                throw new WorkspaceFault(409,"UPLOAD_KEY_CONFLICT");
            await objects.Verify(new(reserved.QuarantineToken!.Value,length,reserved.QuarantineSha256!),timeout.Token);
            return View(reserved);
        }
        try
        {
            var stored=await objects.Write(reserved.QuarantineToken!.Value,input,timeout.Token);
            if(stored.Token!=reserved.QuarantineToken || stored.Bytes is <1 or >QuarantineLimits.MaxBytes || stored.Sha256.Length!=64 || !stored.Sha256.All(Uri.IsHexDigit))
                throw new IOException("QUARANTINE_BINDING_INVALID");
            await objects.Verify(stored,timeout.Token);
            return await Complete(actor,reserved,stored,timeout.Token);
        }
        catch(Exception error)
        {
            // Do not delete a final object after an ambiguous SQL commit. Maintenance checks the DB binding.
            using var failureTimeout=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            try {await Fail(actor,reserved,error is OperationCanceledException?"UPLOAD_INTERRUPTED":error is WorkspaceFault f?f.Message:"UPLOAD_FAILED",failureTimeout.Token);} catch { /* Upload lease/orphan maintenance is durable fallback. */ }
            throw;
        }
    }
    private async Task<QuarantineVersionView> Complete(OwnerActor actor,DocumentVersionRow reserved,StoredQuarantine stored,CancellationToken ct)
    {
        await using var db=new GatewayDbContext(options);await using var tx=await db.Database.BeginTransactionAsync(ct);
        var doc=await Owned(db,actor,reserved.DocumentId,ct);
        var row=await db.DocumentVersions.SingleAsync(x=>x.Id==reserved.Id && x.TenantId==actor.Principal.TenantId,ct);
        if(row.QuarantineToken!=stored.Token || row.State!=IntakeState.Uploading || row.UploadLeaseUntil<=Now)
            throw new WorkspaceFault(409,"UPLOAD_LEASE_LOST");
        if(!doc.Enabled || doc.Status!=PublicationStatus.Draft || doc.CurrentPublishedVersionId is not null)
            throw new WorkspaceFault(409,"SOURCE_OWNER_VERIFICATION_REQUIRED");
        row.QuarantineBytes=stored.Bytes;row.QuarantineSha256=stored.Sha256;row.UploadLeaseUntil=null;row.State=IntakeState.AwaitingValidation;
        Audit(db,doc,actor.Principal.ObjectId,"QuarantineUploadReceived");
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);return View(row);
    }
    private async Task Fail(OwnerActor actor,DocumentVersionRow reserved,string code,CancellationToken ct)
    {
        await using var db=new GatewayDbContext(options);await using var tx=await db.Database.BeginTransactionAsync(ct);
        var doc=await Owned(db,actor,reserved.DocumentId,ct);
        var row=await db.DocumentVersions.SingleAsync(x=>x.Id==reserved.Id,ct);
        if(row.QuarantineToken!=reserved.QuarantineToken || row.State!=IntakeState.Uploading) return;
        row.State=IntakeState.ValidationFailed;row.UploadLeaseUntil=null;row.UploadErrorCode=code;
        Audit(db,doc,actor.Principal.ObjectId,"QuarantineUploadFailed");await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
    }
    public async Task<QuarantineVersionView[]> List(OwnerActor actor,Guid document,CancellationToken ct=default)
    {
        Authorize(actor);await using var db=new GatewayDbContext(options);
        if(!await db.Documents.AnyAsync(x=>x.Id==document && x.TenantId==actor.Principal.TenantId && x.OwnerOid==actor.Principal.ObjectId,ct))
            throw new WorkspaceFault(404,"DOCUMENT_NOT_FOUND");
        return (await db.DocumentVersions.AsNoTracking().Where(x=>x.DocumentId==document && x.TenantId==actor.Principal.TenantId && x.UploadKey!=null)
            .OrderByDescending(x=>x.UploadedAt).ThenBy(x=>x.Id).Take(50).ToArrayAsync(ct)).Select(View).ToArray();
    }
    // Operator-only bounded maintenance; never scans or promotes anything.
    public async Task<int> RecoverExpired(CancellationToken ct=default)
    {
        await using var db=new GatewayDbContext(options);var now=Now;
        var expired=await db.DocumentVersions.AsNoTracking().Where(x=>x.UploadKey!=null && x.State==IntakeState.Uploading && x.UploadLeaseUntil<=now).Take(20).ToArrayAsync(ct);
        var count=0;
        foreach(var row in expired)
        {
            await using var check=new GatewayDbContext(options);
            var owner=await check.Documents.Where(x=>x.Id==row.DocumentId && x.TenantId==row.TenantId).Select(x=>x.OwnerOid).SingleAsync(ct);
            // Fail is token/state-fenced; a renewed attempt cannot be marked abandoned by this old snapshot.
            await Fail(new(new(row.TenantId,owner),true),row,"UPLOAD_ABANDONED",ct);count++;
        }
        return count;
    }
    public async Task<int> CleanupOrphans(CancellationToken ct=default)
    {
        var count=0;
        foreach(var candidate in objects.Candidates(1000).Where(x=>x.ModifiedAt<Now.AddHours(-24)).DistinctBy(x=>x.Token))
        {
            await using var db=new GatewayDbContext(options);var now=Now;
            // Keep every bound unprocessed envelope, even if old; no successful receipt loses its bytes.
            var retain=await db.DocumentVersions.AnyAsync(x=>x.QuarantineToken==candidate.Token && (x.State==IntakeState.AwaitingValidation || x.State==IntakeState.Uploading && x.UploadLeaseUntil>now),ct);
            if(retain) continue;await objects.Delete(candidate.Token,ct);count++;
        }
        return count;
    }
}
