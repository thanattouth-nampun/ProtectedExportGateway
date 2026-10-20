using Gateway.Application;
using Gateway.Domain;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Gateway.SqlTests;

// Each test creates an isolated local database. No cloud/env connection strings are accepted.
public sealed class QueueTests : IAsyncLifetime
{
    private readonly string database = "GatewayQueueTest_" + Guid.NewGuid().ToString("N");
    private DbContextOptions<GatewayDbContext> options = null!;
    private SqlExportQueue Queue => new(options);
    private readonly PrincipalId requester = new(Guid.NewGuid(), Guid.NewGuid());
    public async Task InitializeAsync()
    {
        var root = Environment.GetEnvironmentVariable("GATEWAY_SQL_TEST_ROOT")
            ?? throw new InvalidOperationException("Run scripts/verify-sql.ps1; SQL tests require an explicit workspace runtime root.");
        var path = Path.Combine(Path.GetFullPath(root), database + ".mdf");
        options = new DbContextOptionsBuilder<GatewayDbContext>().UseSqlServer(
            $"Server=(localdb)\\MSSQLLocalDB;Database={database};AttachDbFilename={path};Integrated Security=true;Encrypt=false",
            sql => sql.UseCompatibilityLevel(160)).Options;
        await using var db = new GatewayDbContext(options);
        try { await db.Database.MigrateAsync(); }
        catch { await db.Database.EnsureDeletedAsync(); throw; }
    }
    public async Task DisposeAsync()
    {
        if (options is null) return;
        await using var db = new GatewayDbContext(options); await db.Database.EnsureDeletedAsync();
    }
    private ExportSubmission Submission(PrincipalId? actor = null, string policy = "policy-v1", ExecutionMode mode = ExecutionMode.Simulated)
        => ExportSubmission.Report(actor ?? requester, mode, "headcount-summary", "2026-09", "synthetic-v1", new string('A',64), policy,"label-only-v1","test-label");
    private async Task<Guid> Enqueue(ExportSubmission? s = null, Guid? key = null)
        => (await Queue.EnqueueAsync(s ?? Submission(), key ?? Guid.NewGuid(), Guid.NewGuid())).JobId;
    private async Task Expire(JobLease lease)
    {
        await using var db = new GatewayDbContext(options);
        await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ExportJobs] SET [LeaseExpiresAt]=DATEADD(second,-1,SYSUTCDATETIME()) WHERE [Id]={lease.JobId}");
    }

    [Fact] public async Task Migrations_reapply_without_drift_and_snapshot_survives_context_restart()
    {
        var snapshot = Submission(); var id = await Enqueue(snapshot);
        await using var db = new GatewayDbContext(options); await db.Database.MigrateAsync();
        Assert.False(db.Database.HasPendingModelChanges());
        var restored = await new SqlExportQueue(options).FindForRequesterAsync(id, requester);
        Assert.Equal(snapshot.SnapshotJson, restored!.SnapshotJson); Assert.Equal(snapshot.Digest, restored.RequestDigest);
        Assert.Single(await db.AuditEvents.Where(x => x.JobId == id).ToListAsync());
    }
    [Fact] public async Task Concurrent_same_request_replays_once_with_one_audit()
    {
        var key = Guid.NewGuid(); var snapshot = Submission();
        var results = await Task.WhenAll(Enumerable.Range(0,6).Select(_ => Queue.EnqueueAsync(snapshot,key,Guid.NewGuid())));
        Assert.Single(results.Select(x => x.JobId).Distinct()); Assert.Single(results, x => !x.Replayed);
        await using var db = new GatewayDbContext(options); Assert.Equal(1, await db.ExportJobs.CountAsync());
        Assert.Equal(1, await db.AuditEvents.CountAsync());
    }
    [Fact] public async Task Empty_test_database_can_roll_back_all_migrations_and_reapply()
    {
        await using var db = new GatewayDbContext(options);
        await db.GetService<IMigrator>().MigrateAsync("0");
        Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
        await db.Database.MigrateAsync();
        Assert.Equal(4,(await db.Database.GetAppliedMigrationsAsync()).Count());
        Assert.Equal(0,await db.ExportJobs.CountAsync());
    }
    [Fact] public async Task Changed_payload_conflicts_and_same_key_other_requester_or_tenant_is_isolated()
    {
        var key = Guid.NewGuid(); var a = await Enqueue(key:key);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => Enqueue(Submission(policy:"v2"),key));
        Assert.Equal("IDEMPOTENCY_CONFLICT",error.Message);
        var b = new PrincipalId(requester.TenantId,Guid.NewGuid()); var c = new PrincipalId(Guid.NewGuid(),requester.ObjectId);
        Assert.NotEqual(a, await Enqueue(Submission(b),key)); Assert.NotEqual(a,await Enqueue(Submission(c),key));
        Assert.Null(await Queue.FindForRequesterAsync(a,b)); Assert.Null(await Queue.FindForRequesterAsync(a,c));
    }
    [Fact] public async Task Copy_digest_binds_immutable_source_recipient_policy_rights_and_mode()
    {
        var blob = new BlobBinding("private-source","blob-version","etag",new string('A',64),100);
        var mapping = new RecipientMapping(requester,"a@example.test","map-v1",DateTimeOffset.UtcNow,true);
        var copy = new CopySnapshot(requester,Guid.NewGuid(),Guid.NewGuid(),blob,requester,"udp-label","rights-v1",mapping,"policy-v1","processor@example.test");
        var key = Guid.NewGuid(); var id = await Enqueue(ExportSubmission.Copy(copy,ExecutionMode.Simulated),key);
        foreach (var changed in new[] { copy with { Source=blob with { VersionId="other-version" } }, copy with { RightsVersion="v2" },
            copy with { Recipient=mapping with { Version="map-v2" } },copy with { PolicyVersion="v2" } })
            await Assert.ThrowsAsync<InvalidOperationException>(() => Enqueue(ExportSubmission.Copy(changed,ExecutionMode.Simulated),key));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Enqueue(ExportSubmission.Copy(copy,ExecutionMode.LiveMip),key));
        Assert.Equal(SourceKind.DocumentCopy,(await Queue.FindForRequesterAsync(id,requester))!.SourceKind);
    }
    [Fact] public async Task Concurrent_claims_assign_one_attempt_per_job()
    {
        await Enqueue(); var claims = await Task.WhenAll(Enumerable.Range(0,5).Select(i => Queue.ClaimAsync("worker-"+i,TimeSpan.FromSeconds(30))));
        Assert.Single(claims, x => x is not null);
        await using var db = new GatewayDbContext(options); Assert.Equal(1,await db.ExportAttempts.CountAsync());
    }
    [Fact] public async Task Expired_worker_cannot_renew_or_move_even_before_reclaim()
    {
        await Enqueue(); var lease = (await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)))!;
        await Expire(lease); Assert.False(await Queue.RenewAsync(lease,TimeSpan.FromSeconds(30)));
        Assert.False(await Queue.MoveAsync(lease,JobState.Preparing,JobState.Labeling));
    }
    [Fact] public async Task Reclaim_increments_fence_closes_old_attempt_and_rejects_stale_or_forged_writer()
    {
        await Enqueue(); var old = (await Queue.ClaimAsync("first",TimeSpan.FromSeconds(30)))!;
        await Expire(old); var current = (await Queue.ClaimAsync("second",TimeSpan.FromSeconds(30)))!;
        Assert.Equal(old.Fence+1,current.Fence); Assert.Equal(2,current.Attempt);
        Assert.False(await Queue.MoveAsync(old,JobState.Preparing,JobState.Labeling));
        Assert.False(await Queue.RenewAsync(current with { Token=Guid.NewGuid() },TimeSpan.FromSeconds(30)));
        Assert.False(await Queue.MoveAsync(current with { Worker="first" },JobState.Preparing,JobState.Labeling));
        Assert.False(await Queue.MoveAsync(current with { Worker="SECOND" },JobState.Preparing,JobState.Labeling));
        await Assert.ThrowsAsync<ArgumentException>(()=>Queue.MoveAsync(current with { Worker="second " },JobState.Preparing,JobState.Labeling));
        Assert.True(await Queue.MoveAsync(current,JobState.Preparing,JobState.Labeling));
        await using var db = new GatewayDbContext(options);
        Assert.Equal("LeaseExpired",(await db.ExportAttempts.SingleAsync(x=>x.Fence==old.Fence)).Outcome);
        Assert.DoesNotContain(await db.AuditEvents.Select(x=>x.CorrelationId).ToListAsync(), x=>x==current.Token || x==old.Token);
    }
    [Fact] public async Task Simulated_pipeline_finishes_without_lease_and_has_no_ready_transition()
    {
        var id = await Enqueue(); var lease = (await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)))!;
        Assert.True(await Queue.RenewAsync(lease,TimeSpan.FromSeconds(60)));
        Assert.True(await Queue.MoveAsync(lease,JobState.Preparing,JobState.Labeling));
        Assert.False(await Queue.MoveAsync(lease,JobState.Preparing,JobState.Labeling));
        Assert.True(await Queue.MoveAsync(lease,JobState.Labeling,JobState.Verifying));
        Assert.True(await Queue.MoveAsync(lease,JobState.Verifying,JobState.Simulated));
        Assert.Null(await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)));
        var job = await Queue.FindForRequesterAsync(id,requester); Assert.Null(job!.LeaseToken); Assert.Equal(JobState.Simulated,job.State);
        await Assert.ThrowsAsync<InvalidOperationException>(()=>Queue.MoveAsync(lease,JobState.Verifying,JobState.Ready));
        await using var db = new GatewayDbContext(options);
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(()=>db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ExportJobs] SET [State]=5 WHERE [Id]={id}"));
    }
    [Fact] public async Task Live_job_cannot_be_completed_as_simulated()
    {
        await Enqueue(Submission(mode:ExecutionMode.LiveMip)); var lease = (await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)))!;
        await Queue.MoveAsync(lease,JobState.Preparing,JobState.Labeling); await Queue.MoveAsync(lease,JobState.Labeling,JobState.Verifying);
        Assert.False(await Queue.MoveAsync(lease,JobState.Verifying,JobState.Simulated));
    }
    [Fact] public async Task Retries_back_off_and_exhaustion_fails_closed()
    {
        var id = await Enqueue();
        for (var i=0;i<3;i++)
        {
            var lease = (await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)))!;
            if (i==0) {
                Assert.True(await Queue.MoveAsync(lease,JobState.Preparing,JobState.RetryPending));
                Assert.Null(await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)));
                await using var adjust = new GatewayDbContext(options);
                await adjust.Database.ExecuteSqlInterpolatedAsync($"UPDATE [ExportJobs] SET [AvailableAt]=SYSUTCDATETIME() WHERE [Id]={id}");
            } else await Expire(lease);
        }
        Assert.Null(await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)));
        Assert.Equal(1,await Queue.FailExhaustedAsync()); Assert.Equal(0,await Queue.FailExhaustedAsync());
        Assert.Equal(JobState.Failed,(await Queue.FindForRequesterAsync(id,requester))!.State);
    }
    [Fact] public async Task Audit_failure_rolls_back_state_and_attempt_changes()
    {
        var id = await Enqueue(); var lease = (await Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)))!;
        await using var db = new GatewayDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER [RejectAudit] ON [AuditEvents] INSTEAD OF INSERT AS THROW 51000, 'TEST_AUDIT_UNAVAILABLE', 1;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Queue.MoveAsync(lease,JobState.Preparing,JobState.Failed));
        var job = await Queue.FindForRequesterAsync(id,requester); Assert.Equal(JobState.Preparing,job!.State); Assert.Equal(lease.Token,job.LeaseToken);
        Assert.Null((await db.ExportAttempts.SingleAsync()).EndedAt);
    }
    [Fact] public async Task Audit_failure_during_enqueue_leaves_no_job_or_attempt()
    {
        await using var db = new GatewayDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER [RejectAudit] ON [AuditEvents] INSTEAD OF INSERT AS THROW 51000, 'TEST_AUDIT_UNAVAILABLE', 1;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Enqueue());
        Assert.Equal(0,await db.ExportJobs.CountAsync()); Assert.Equal(0,await db.ExportAttempts.CountAsync());
    }
    [Fact] public async Task Audit_failure_during_claim_rolls_back_lease_and_attempt()
    {
        var id = await Enqueue();
        await using var db = new GatewayDbContext(options);
        await db.Database.ExecuteSqlRawAsync("CREATE TRIGGER [RejectAudit] ON [AuditEvents] INSTEAD OF INSERT AS THROW 51000, 'TEST_AUDIT_UNAVAILABLE', 1;");
        await Assert.ThrowsAsync<DbUpdateException>(()=>Queue.ClaimAsync("worker",TimeSpan.FromSeconds(30)));
        var job = await Queue.FindForRequesterAsync(id,requester); Assert.Equal(JobState.Queued,job!.State);
        Assert.Equal(0,job.Fence); Assert.Null(job.LeaseToken); Assert.Equal(0,await db.ExportAttempts.CountAsync());
    }
    [Fact] public async Task Document_rowversion_and_composite_foreign_keys_reject_lost_updates_and_cross_document_pointer()
    {
        await using var db = new GatewayDbContext(options);
        var doc = new DocumentRow { Id=Guid.NewGuid(),TenantId=requester.TenantId,OwnerOid=requester.ObjectId,Title="Synthetic",Purpose="Test" };
        var other = new DocumentRow { Id=Guid.NewGuid(),TenantId=requester.TenantId,OwnerOid=requester.ObjectId,Title="Other",Purpose="Test" };
        db.Documents.AddRange(doc,other); await db.SaveChangesAsync();
        await using var stale = new GatewayDbContext(options); var staleDoc = await stale.Documents.SingleAsync(x=>x.Id==doc.Id);
        doc.Title="Changed"; await db.SaveChangesAsync(); staleDoc.Title="Stale";
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(()=>stale.SaveChangesAsync());
        var version = new DocumentVersionRow { Id=Guid.NewGuid(),DocumentId=other.Id,TenantId=other.TenantId,UploaderOid=other.OwnerOid,DisplayFilename="fixture.xlsx",UploadedAt=DateTimeOffset.UtcNow };
        db.DocumentVersions.Add(version); await db.SaveChangesAsync(); doc.CurrentPublishedVersionId=version.Id;
        await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());
    }
}
