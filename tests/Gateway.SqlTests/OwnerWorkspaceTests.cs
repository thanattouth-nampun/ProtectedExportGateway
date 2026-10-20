using Gateway.Application;
using Gateway.Domain;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Gateway.SqlTests;

public sealed class OwnerWorkspaceTests:IAsyncLifetime
{
    private readonly Guid tenant=Guid.NewGuid(); private readonly Guid owner=Guid.NewGuid(); private readonly Guid a=Guid.NewGuid(); private readonly Guid other=Guid.NewGuid();
    private DbContextOptions<GatewayDbContext> options=null!;
    private PortalIdentityDirectory directory=null!;
    private OwnerActor Actor=>new(new(tenant,owner),true);
    private SqlOwnerWorkspace Store=>new(options,directory);
    private DraftEdit Edit(Guid[]? ids=null,string title="Synthetic draft")=>new(title,"Synthetic planning metadata",ids ?? [a]);
    public async Task InitializeAsync()
    {
        var root=Environment.GetEnvironmentVariable("GATEWAY_SQL_TEST_ROOT") ?? throw new InvalidOperationException("Use verify-sql.ps1");
        var database="GatewayOwnerTest_"+Guid.NewGuid().ToString("N");
        options=new DbContextOptionsBuilder<GatewayDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};AttachDbFilename={Path.Combine(Path.GetFullPath(root),database+".mdf")};Integrated Security=true;Encrypt=false",s=>s.UseCompatibilityLevel(160)).Options;
        directory=new(tenant,[new(owner,"Owner","owner@example.test","sql-test"),new(a,"A","a@example.test","sql-test"),new(other,"Other owner","other@example.test","sql-test")]);
        await using var db=new GatewayDbContext(options); await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync() {await using var db=new GatewayDbContext(options); await db.Database.EnsureDeletedAsync();}
    [Fact] public async Task Draft_and_acl_survive_restart_with_document_bound_audit_and_no_source()
    {
        var created=await Store.Create(Actor,Edit(),Guid.NewGuid());
        var read=await new SqlOwnerWorkspace(options,directory).Find(Actor,created.Id);
        Assert.Equal(created,read with {RequesterObjectIds=created.RequesterObjectIds}); Assert.Equal(a,Assert.Single(read.RequesterObjectIds));
        Assert.False(read.CanPublish); Assert.Equal("NoSourceUploaded",read.SourceOwnershipStatus); Assert.Equal("Draft",read.Status);
        await using var db=new GatewayDbContext(options); Assert.Equal(owner,(await db.Documents.SingleAsync()).OwnerOid);
        var audit=await db.AuditEvents.SingleAsync(); Assert.Equal(created.Id,audit.DocumentId); Assert.Equal(1,audit.DocumentRevision); Assert.Equal(owner,audit.ActorOid);
        Assert.Empty(await db.DocumentVersions.ToListAsync()); Assert.Empty(await db.ExportJobs.ToListAsync());
    }
    [Fact] public async Task Metadata_acl_and_revisions_update_atomically_and_stale_write_changes_nothing()
    {
        var doc=await Store.Create(Actor,Edit(),Guid.NewGuid());
        var updated=await Store.Update(Actor,doc.Id,doc.ETag,Edit([],"Updated title"),Guid.NewGuid());
        Assert.Empty(updated.RequesterObjectIds); Assert.Equal(2,updated.Revision); Assert.Equal(2,updated.AclRevision); Assert.NotEqual(doc.ETag,updated.ETag);
        Assert.Equal(412,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Update(Actor,doc.Id,doc.ETag,Edit(),Guid.NewGuid()))).Status);
        var unchanged=await Store.Find(Actor,doc.Id); Assert.Equal("Updated title",unchanged.Title); Assert.Empty(unchanged.RequesterObjectIds);
        Assert.Equal(2,(await Store.Audit(Actor,doc.Id)).Length);
    }
    [Fact] public async Task Concurrent_edits_have_one_winner_and_one_stale_failure()
    {
        var doc=await Store.Create(Actor,Edit(),Guid.NewGuid());
        async Task<int> Update(string title) {try {await Store.Update(Actor,doc.Id,doc.ETag,Edit([],title),Guid.NewGuid());return 200;} catch(WorkspaceFault e){return e.Status;}}
        var results=await Task.WhenAll(Update("One"),Update("Two"));
        Assert.Equal(new[]{200,412},results.Order().ToArray()); Assert.Equal(2,(await Store.Audit(Actor,doc.Id)).Length);
    }
    [Fact] public async Task Unverified_member_other_owner_and_other_tenant_cannot_read_or_mutate()
    {
        var doc=await Store.Create(Actor,Edit(),Guid.NewGuid());
        Assert.Equal(403,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.List(Actor with {IsWorkspaceMember=false},0))).Status);
        var foreignOwner=new OwnerActor(new(tenant,other),true);
        Assert.Empty((await Store.List(foreignOwner,0)).Items);
        Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Find(foreignOwner,doc.Id))).Status);
        Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Update(foreignOwner,doc.Id,doc.ETag,Edit(),Guid.NewGuid()))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Find(new(new(Guid.NewGuid(),owner),true),doc.Id))).Status);
    }
    [Fact] public async Task Every_verified_member_has_own_private_workspace_even_without_recipient_directory_entry()
    {
        var member=new OwnerActor(new(tenant,Guid.NewGuid()),true);
        var first=await Store.Create(Actor,Edit(),Guid.NewGuid());
        var second=await Store.Create(member,Edit([],"Member private draft"),Guid.NewGuid());
        Assert.Equal(second.Id,Assert.Single((await Store.List(member,0)).Items).Id);
        Assert.Equal(first.Id,Assert.Single((await Store.List(Actor,0)).Items).Id);
        foreach(var pair in new[]{(actor:Actor,document:second),(actor:member,document:first)})
        {
            Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Find(pair.actor,pair.document.Id))).Status);
            Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Update(pair.actor,pair.document.Id,pair.document.ETag,Edit([]),Guid.NewGuid()))).Status);
            Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Audit(pair.actor,pair.document.Id))).Status);
        }
        Assert.Empty(second.RequesterObjectIds); Assert.False(second.CanPublish);
        Assert.Single(await Store.Audit(member,second.Id));
    }
    [Fact] public async Task Unreviewed_or_duplicate_acl_rejects_without_any_database_write()
    {
        foreach (var ids in new[]{new[]{Guid.NewGuid()},new[]{a,a}})
            Assert.Equal(400,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Create(Actor,Edit(ids),Guid.NewGuid()))).Status);
        await using var db=new GatewayDbContext(options); Assert.Empty(await db.Documents.ToListAsync()); Assert.Empty(await db.AuditEvents.ToListAsync());
    }
    [Fact] public async Task Audit_failure_rolls_back_document_and_acl()
    {
        await using(var db=new GatewayDbContext(options)) await db.Database.ExecuteSqlRawAsync("ALTER TABLE [AuditEvents] ADD CONSTRAINT [CK_Test_RejectDraftAudit] CHECK ([EventType] <> N'DocumentDraftCreated')");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Store.Create(Actor,Edit(),Guid.NewGuid()));
        await using var check=new GatewayDbContext(options); Assert.Empty(await check.Documents.ToListAsync()); Assert.Empty(await check.DocumentAcls.ToListAsync());
    }
    [Fact] public async Task Source_presence_blocks_metadata_mutation_until_owner_binding_is_implemented()
    {
        var doc=await Store.Create(Actor,Edit(),Guid.NewGuid());
        await using(var db=new GatewayDbContext(options)) {db.DocumentVersions.Add(new(){Id=Guid.NewGuid(),DocumentId=doc.Id,TenantId=tenant,UploaderOid=owner,DisplayFilename="synthetic.xlsx",State=IntakeState.AwaitingValidation,UploadedAt=DateTimeOffset.UtcNow});await db.SaveChangesAsync();}
        Assert.Equal(409,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Update(Actor,doc.Id,doc.ETag,Edit(),Guid.NewGuid()))).Status);
        Assert.False((await Store.Find(Actor,doc.Id)).CanPublish); Assert.Single(await Store.Audit(Actor,doc.Id));
    }
}
