using Gateway.Application;
using Gateway.Domain;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using Xunit;

namespace Gateway.SqlTests;

// Real SQL + private local files, synthetic opaque bytes; never protection/scanner/OOXML evidence.
public sealed class QuarantineTests:IAsyncLifetime
{
    private readonly Guid tenant=Guid.NewGuid(),owner=Guid.NewGuid(),other=Guid.NewGuid();
    private DbContextOptions<GatewayDbContext> options=null!;private PortalIdentityDirectory directory=null!;
    private LocalQuarantineObjects objects=null!;private string root="";
    private readonly TestClock clock=new();
    private OwnerActor Actor=>new(new(tenant,owner),true);
    private SqlOwnerWorkspace Owners=>new(options,directory);
    private SqlQuarantineIntake Store=>new(options,directory,objects,clock);
    public async Task InitializeAsync()
    {
        if(!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException();
        var testRoot=Environment.GetEnvironmentVariable("GATEWAY_SQL_TEST_ROOT") ?? throw new InvalidOperationException("LocalDB test root required");
        var id=Guid.NewGuid().ToString("N");root=Path.Combine(testRoot,"quarantine-"+id);Directory.CreateDirectory(root);
        var acl=new DirectorySecurity();acl.SetAccessRuleProtection(true,false);
        acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User!,FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit|InheritanceFlags.ObjectInherit,PropagationFlags.None,AccessControlType.Allow));
        new DirectoryInfo(root).SetAccessControl(acl);objects=new(root);
        options=new DbContextOptionsBuilder<GatewayDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=QuarantineTest_{id};AttachDbFilename={Path.Combine(Path.GetFullPath(testRoot),id+".mdf")};Integrated Security=true;Encrypt=false",s=>s.UseCompatibilityLevel(160)).Options;
        directory=new(tenant,[new(owner,"Owner","owner@example.test","test"),new(other,"Other","other@example.test","test")]);
        await using var db=new GatewayDbContext(options);await db.Database.MigrateAsync();
    }
    public async Task DisposeAsync()
    {
        await using var db=new GatewayDbContext(options);await db.Database.EnsureDeletedAsync();
        var expected=Path.GetFullPath(Environment.GetEnvironmentVariable("GATEWAY_SQL_TEST_ROOT")!)+Path.DirectorySeparatorChar;
        if(!Path.GetFullPath(root).StartsWith(expected,StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe test cleanup");
        Directory.Delete(root,true);
    }
    private Task<OwnerDocumentView> Draft()=>Owners.Create(Actor,new("Synthetic quarantine","Test envelope only",[]),Guid.NewGuid());
    private Task<QuarantineVersionView> Upload(OwnerDocumentView doc,Guid key,byte[] bytes)=>Store.Upload(Actor,doc.Id,doc.ETag,key,"synthetic.xlsx",true,new MemoryStream(bytes));
    private static readonly string ProbeLabel=Guid.NewGuid().ToString();
    private SqlQuarantineOwnershipProbe Probe(PortalIdentityDirectory? mapping=null)=>new(options,mapping??directory,objects);
    private sealed class Inspector(Func<Stream,string,string,CancellationToken,Task<ProtectedFileInspection>> inspect):IProtectedOwnershipInspector
    {
        public int Calls {get;private set;}
        public Task<ProtectedFileInspection> Inspect(Stream file,string name,string owner,CancellationToken ct)
        {Calls++;return inspect(file,name,owner,ct);}
    }
    private static Inspector Observed(string owner)=>new((_,_,_,_)=>Task.FromResult(new ProtectedFileInspection(true,ProbeLabel,owner,[],false)));
    [Fact] public async Task Ownership_preflight_pins_receipt_without_inspecting_or_changing_SQL()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1,2,3]);
        var report=await Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,null);
        Assert.Equal("LOCAL_PREFLIGHT_ONLY_INSPECTION_NOT_RUN",report.Outcome);Assert.False(report.GatePassed);Assert.False(report.CanPublish);
        Assert.Equal(owner,report.UploaderOid);Assert.Equal(receipt,Assert.Single(await Store.List(Actor,doc.Id)));
        Assert.Equal(3,(await Owners.Audit(Actor,doc.Id)).Length);
    }
    [Fact] public async Task Ownership_simulation_uses_reviewed_mapping_readlocked_bytes_and_preserves_pending_state()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1,2,3]);
        var inspector=new Inspector((file,name,address,_)=>
        {
            Assert.Equal("owner@example.test",address);Assert.False(file.CanWrite);Assert.Equal(0,file.Position);
            Assert.EndsWith(".xlsx",name);Assert.DoesNotContain("\\",name);Assert.Equal(1,file.ReadByte());
            return Task.FromResult(new ProtectedFileInspection(true,ProbeLabel,address,[],false));
        });
        var report=await Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,inspector);
        Assert.Equal("OWNER_ONLY_OBSERVED_NOT_ELIGIBLE",report.Outcome);Assert.False(report.CanPublish);
        await using var db=new GatewayDbContext(options);var row=await db.DocumentVersions.SingleAsync();
        Assert.Equal(IntakeState.AwaitingValidation,row.State);Assert.Null(row.SourceBindingJson);Assert.Null(row.ScanEvidenceJson);
    }
    [Fact] public async Task Ownership_simulation_rejects_original_owner_file_uploaded_by_other_person()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);
        var report=await Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,Observed("other@example.test"));
        Assert.Equal("RMS_OWNER_MISMATCH",report.Outcome);Assert.False(report.CanPublish);
    }
    [Fact] public async Task Ownership_unmapped_member_never_invokes_inspector()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);var inspector=Observed("owner@example.test");
        var onlyOther=new PortalIdentityDirectory(tenant,[new(other,"Other","other@example.test","review")]);
        var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>Probe(onlyOther).Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,inspector));
        Assert.Equal("UPLOADER_RMS_MAPPING_NOT_REVIEWED",e.Message);Assert.Equal(0,inspector.Calls);
    }
    [Fact] public async Task Ownership_wrong_target_hash_or_tenant_never_invokes_inspector()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);var inspector=Observed("owner@example.test");
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Probe().Run(tenant,receipt.Id,new('F',64),ProbeLabel,inspector));
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Probe().Run(Guid.NewGuid(),receipt.Id,receipt.Sha256!,ProbeLabel,inspector));
        Assert.Equal(0,inspector.Calls);
    }
    [Fact] public async Task Ownership_tampered_private_object_never_invokes_inspector()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);var inspector=Observed("owner@example.test");
        await File.WriteAllBytesAsync(Assert.Single(Directory.GetFiles(root)),[2]);
        var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,inspector));
        Assert.Equal("QUARANTINE_BYTES_CHANGED",e.Message);Assert.Equal(0,inspector.Calls);
    }
    [Theory][InlineData(true)][InlineData(false)]
    public async Task Ownership_concurrent_document_or_version_change_invalidates_observation(bool changeDoc)
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);
        var inspector=new Inspector(async (_,_,address,_)=>
        {
            await using var db=new GatewayDbContext(options);
            if(changeDoc) (await db.Documents.SingleAsync()).Enabled=false;
            else (await db.DocumentVersions.SingleAsync()).UploadErrorCode="CONCURRENT_TEST_CHANGE";
            await db.SaveChangesAsync();return new(true,ProbeLabel,address,[],false);
        });
        var e=await Assert.ThrowsAsync<InvalidOperationException>(()=>Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,inspector));
        Assert.Equal("QUARANTINE_SNAPSHOT_CHANGED",e.Message);
    }
    [Fact] public async Task Ownership_inspector_failure_does_not_write_success_or_audit()
    {
        var doc=await Draft();var receipt=await Upload(doc,Guid.NewGuid(),[1]);
        var inspector=new Inspector((_,_,_,_)=>throw new IOException("synthetic SDK unavailable"));
        await Assert.ThrowsAsync<IOException>(()=>Probe().Run(tenant,receipt.Id,receipt.Sha256!,ProbeLabel,inspector));
        Assert.Equal(receipt,Assert.Single(await Store.List(Actor,doc.Id)));Assert.Equal(3,(await Owners.Audit(Actor,doc.Id)).Length);
    }
    [Fact] public async Task Receipt_is_durable_hash_measured_and_never_eligible_or_source_bound()
    {
        var doc=await Draft();byte[] bytes=[1,2,3,4];var received=await Upload(doc,Guid.NewGuid(),bytes);
        Assert.Equal("AwaitingValidation",received.State);Assert.Equal(4,received.Bytes);Assert.Equal(Convert.ToHexString(SHA256.HashData(bytes)),received.Sha256);
        Assert.False(received.CanPublish);Assert.Equal("LocalQuarantineSpike",received.StorageMode);
        Assert.Equal(received,Assert.Single(await new SqlQuarantineIntake(options,directory,objects,clock).List(Actor,doc.Id)));
        await using var db=new GatewayDbContext(options);var row=await db.DocumentVersions.SingleAsync();
        Assert.Null(row.SourceBindingJson);Assert.Null(row.ScanEvidenceJson);Assert.Null(row.ValidationEvidenceJson);Assert.Null(row.UploadLeaseUntil);
        Assert.Equal(3,(await Owners.Find(Actor,doc.Id)).Revision);Assert.Equal(3,(await Owners.Audit(Actor,doc.Id)).Length);
        Assert.False((await Owners.Find(Actor,doc.Id)).CanPublish);Assert.Single(Directory.GetFiles(root));
        row.State=IntakeState.Eligible;await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
    }
    [Fact] public async Task Same_key_same_measured_bytes_replays_but_other_bytes_or_name_conflict()
    {
        var doc=await Draft();var key=Guid.NewGuid();var first=await Upload(doc,key,[4,5]);
        Assert.Equal(first,await Upload(doc,key,[4,5])); // Original stale ETag is allowed only for bound replay.
        Assert.Equal(409,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(doc,key,[4,6]))).Status);
        Assert.Equal(409,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Upload(Actor,doc.Id,doc.ETag,key,"changed.xlsx",true,new MemoryStream([4,5])))).Status);
        Assert.Single(await Store.List(Actor,doc.Id));Assert.Equal(3,(await Owners.Audit(Actor,doc.Id)).Length);
    }
    [Fact] public async Task Concurrent_same_key_cannot_duplicate_and_stale_new_key_cannot_reserve()
    {
        var doc=await Draft();var key=Guid.NewGuid();using var stream=new BlockingStream([1,2]);
        var first=Store.Upload(Actor,doc.Id,doc.ETag,key,"synthetic.xlsx",true,stream);await stream.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
        Assert.Equal("UPLOAD_IN_PROGRESS",(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(doc,key,[1,2]))).Message);
        Assert.Equal(412,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(doc,Guid.NewGuid(),[1]))).Status);
        stream.Release.SetResult();await first;Assert.Single(await Store.List(Actor,doc.Id));
    }
    [Fact] public async Task Other_owner_foreign_tenant_and_unacknowledged_upload_never_read_body()
    {
        var doc=await Draft();using var input=new ThrowingStream();
        var stranger=new OwnerActor(new(tenant,other),true);
        Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Upload(stranger,doc.Id,doc.ETag,Guid.NewGuid(),"synthetic.xlsx",true,input))).Status);
        Assert.Equal(404,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.List(stranger,doc.Id))).Status);
        Assert.Equal(403,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Upload(new(new(Guid.NewGuid(),owner),true),doc.Id,doc.ETag,Guid.NewGuid(),"synthetic.xlsx",true,input))).Status);
        Assert.Equal(400,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Upload(Actor,doc.Id,doc.ETag,Guid.NewGuid(),"synthetic.xlsx",false,input))).Status);
        Assert.Empty(await Store.List(Actor,doc.Id));Assert.Empty(Directory.GetFiles(root));
    }
    [Theory][InlineData(0,400)][InlineData(10485761,413)]
    public async Task Empty_or_oversized_bytes_never_bind_and_part_file_is_removed(int length,int status)
    {
        var doc=await Draft();Assert.Equal(status,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(doc,Guid.NewGuid(),new byte[length]))).Status);
        var failed=Assert.Single(await Store.List(Actor,doc.Id));Assert.Equal("ValidationFailed",failed.State);Assert.Null(failed.Sha256);Assert.Empty(Directory.GetFiles(root));
    }
    [Fact] public async Task Interrupted_upload_retries_same_key_with_latest_ETag_and_fresh_token()
    {
        var doc=await Draft();var key=Guid.NewGuid();
        await Assert.ThrowsAsync<IOException>(()=>Store.Upload(Actor,doc.Id,doc.ETag,key,"synthetic.xlsx",true,new ThrowingStream()));
        var latest=await Owners.Find(Actor,doc.Id);var received=await Upload(latest,key,[7]);
        Assert.Equal("AwaitingValidation",received.State);Assert.Single(await Store.List(Actor,doc.Id));
        await using var db=new GatewayDbContext(options);Assert.Equal(2,(await db.DocumentVersions.SingleAsync()).UploadAttempts);
    }
    [Fact] public async Task Expired_attempt_is_fenced_and_orphan_cleanup_preserves_bound_candidate()
    {
        var doc=await Draft();var key=Guid.NewGuid();using var slow=new BlockingStream([1]);
        var abandoned=Store.Upload(Actor,doc.Id,doc.ETag,key,"synthetic.xlsx",true,slow);await slow.Entered.Task.WaitAsync(TimeSpan.FromSeconds(20));
        clock.Utc=clock.Utc.AddMinutes(6);Assert.Equal(1,await Store.RecoverExpired());
        var renewed=await Upload(await Owners.Find(Actor,doc.Id),key,[2]);
        slow.Release.SetResult();Assert.Equal("UPLOAD_LEASE_LOST",(await Assert.ThrowsAsync<WorkspaceFault>(()=>abandoned)).Message);
        Assert.Equal(renewed,Assert.Single(await Store.List(Actor,doc.Id)));
        clock.Utc=clock.Utc.AddDays(2);Assert.Equal(1,await Store.CleanupOrphans());Assert.Single(Directory.GetFiles(root));
        await using var db=new GatewayDbContext(options);var bound=await db.DocumentVersions.SingleAsync();
        await objects.Verify(new(bound.QuarantineToken!.Value,bound.QuarantineBytes!.Value,bound.QuarantineSha256!),default);
    }
    [Fact] public async Task Missing_or_tampered_bound_bytes_never_replay_successfully()
    {
        var doc=await Draft();var key=Guid.NewGuid();await Upload(doc,key,[8]);
        await File.WriteAllBytesAsync(Assert.Single(Directory.GetFiles(root)),[9]);
        await Assert.ThrowsAsync<IOException>(()=>Upload(doc,key,[8]));
    }
    [Fact] public async Task Audit_failure_before_receiving_bytes_rolls_back_reservation()
    {
        var doc=await Draft();await using(var db=new GatewayDbContext(options))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE AuditEvents ADD CONSTRAINT CK_Test_NoUpload CHECK (EventType <> N'QuarantineUploadStarted')");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Upload(doc,Guid.NewGuid(),[1]));
        Assert.Empty(await Store.List(Actor,doc.Id));Assert.Empty(Directory.GetFiles(root));
    }
    [Fact] public async Task Recent_failures_count_toward_bounded_queue_and_retries_stop_at_three_attempts()
    {
        var doc=await Draft();var key=Guid.NewGuid();
        for(var attempt=0;attempt<3;attempt++)
        {
            var current=await Owners.Find(Actor,doc.Id);
            await Assert.ThrowsAsync<IOException>(()=>Store.Upload(Actor,doc.Id,current.ETag,key,"synthetic.xlsx",true,new ThrowingStream()));
        }
        Assert.Equal("UPLOAD_KEY_TERMINAL",(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(doc,key,[1]))).Message);
        await using(var db=new GatewayDbContext(options))
        {
            for(var i=0;i<19;i++) db.DocumentVersions.Add(new(){Id=Guid.NewGuid(),DocumentId=doc.Id,TenantId=tenant,UploaderOid=owner,
                UploadKey=Guid.NewGuid(),QuarantineToken=Guid.NewGuid(),UploadAttempts=1,State=IntakeState.ValidationFailed,UploadedAt=clock.Utc,DisplayFilename="synthetic.xlsx",UploadErrorCode="UPLOAD_FAILED"});
            await db.SaveChangesAsync();
        }
        var latest=await Owners.Find(Actor,doc.Id);
        Assert.Equal(429,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Upload(latest,Guid.NewGuid(),[1]))).Status);
        clock.Utc=clock.Utc.AddHours(25);Assert.Equal("AwaitingValidation",(await Upload(latest,Guid.NewGuid(),[1])).State);
    }
    [Fact] public async Task SQL_completion_failure_after_object_write_never_binds_and_orphan_is_recoverable()
    {
        var doc=await Draft();await using(var db=new GatewayDbContext(options))
            await db.Database.ExecuteSqlRawAsync("ALTER TABLE AuditEvents ADD CONSTRAINT CK_Test_NoReceipt CHECK (EventType <> N'QuarantineUploadReceived')");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Upload(doc,Guid.NewGuid(),[1,2]));
        var row=Assert.Single(await Store.List(Actor,doc.Id));Assert.Equal("ValidationFailed",row.State);Assert.Null(row.Sha256);Assert.Null(row.Bytes);
        Assert.False((await Owners.Find(Actor,doc.Id)).CanPublish);Assert.Single(Directory.GetFiles(root));
        clock.Utc=clock.Utc.AddDays(2);Assert.Equal(1,await Store.CleanupOrphans());Assert.Empty(Directory.GetFiles(root));
    }
    [Theory][InlineData("../outside.xlsx")][InlineData("test.xlsm")][InlineData("x.xlsx ")][InlineData("C:\\x.xlsx")]
    public async Task Unsafe_or_unsupported_display_name_rejected_before_body(string filename)
    {
        var doc=await Draft();Assert.Equal(400,(await Assert.ThrowsAsync<WorkspaceFault>(()=>Store.Upload(Actor,doc.Id,doc.ETag,Guid.NewGuid(),filename,true,new ThrowingStream()))).Status);
        Assert.Empty(await Store.List(Actor,doc.Id));
    }
    [Fact] public void Local_adapter_refuses_unreviewed_inherited_root_acl()
    {
        var child=Path.Combine(root,"unreviewed");Directory.CreateDirectory(child);
        Assert.Throws<IOException>(()=>new LocalQuarantineObjects(child));
    }
    private sealed class TestClock:TimeProvider {public DateTimeOffset Utc=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Utc;}
    private sealed class ThrowingStream:MemoryStream
    {public override ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)=>ValueTask.FromException<int>(new IOException("Synthetic interrupted transport"));}
    private sealed class BlockingStream(byte[] bytes):MemoryStream(bytes)
    {
        public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,CancellationToken ct=default)
        {Entered.TrySetResult();await Release.Task.WaitAsync(ct);return await base.ReadAsync(buffer,ct);}
    }
}
