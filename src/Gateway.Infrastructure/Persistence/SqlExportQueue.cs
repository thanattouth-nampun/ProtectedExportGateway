using System.Data;
using Gateway.Application;
using Gateway.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Gateway.Infrastructure.Persistence;

public sealed record EnqueueResult(Guid JobId, bool Replayed);
public sealed record JobLease(Guid JobId, string Worker, Guid Token, long Fence, int Attempt, DateTimeOffset ExpiresAt);

// One short-lived context/transaction per operation; no SQL transaction spans native SDK or Blob I/O.
// Deliberately no Ready/artifact publication method until T09/T10 evidence and authorization are implemented.
public sealed class SqlExportQueue(DbContextOptions<GatewayDbContext> options)
{
    public async Task<EnqueueResult> EnqueueAsync(ExportSubmission submission, Guid key, Guid correlationId, CancellationToken ct = default)
    {
        if (key == Guid.Empty || correlationId == Guid.Empty) throw new ArgumentException("REQUEST_INVALID");
        await using var db = new GatewayDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var p = submission.Requester;
        var existing = await db.ExportJobs.FromSqlInterpolated($"""
            SELECT * FROM [ExportJobs] WITH (UPDLOCK, HOLDLOCK)
            WHERE [TenantId]={p.TenantId} AND [RequesterOid]={p.ObjectId} AND [IdempotencyKey]={key}
            """).SingleOrDefaultAsync(ct);
        if (existing is not null)
        {
            if (existing.RequestDigest != submission.Digest) throw new InvalidOperationException("IDEMPOTENCY_CONFLICT");
            await tx.CommitAsync(ct); return new(existing.Id, true);
        }
        var now = await Now(db, ct);
        var job = new ExportJobRow { Id = Guid.NewGuid(), CorrelationId = correlationId, TenantId = p.TenantId, RequesterOid = p.ObjectId,
            IdempotencyKey = key, RequestDigest = submission.Digest, SnapshotJson = submission.SnapshotJson,
            Mode = submission.Mode, SourceKind = submission.Kind, CreatedAt = now, AvailableAt = now };
        db.ExportJobs.Add(job);
        db.AuditEvents.Add(Event(job, "JobQueued", now, correlationId, p.ObjectId));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return new(job.Id, false);
    }

    public async Task<ExportJobRow?> FindForRequesterAsync(Guid id, PrincipalId requester, CancellationToken ct = default)
    {
        await using var db = new GatewayDbContext(options);
        return await db.ExportJobs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id
            && x.TenantId == requester.TenantId && x.RequesterOid == requester.ObjectId, ct);
    }

    public async Task<JobLease?> ClaimAsync(string worker, TimeSpan duration, CancellationToken ct = default)
    {
        ValidateLease(worker, duration);
        await using var db = new GatewayDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var token = Guid.NewGuid();
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = tx.GetDbTransaction();
        command.CommandText = """
            ;WITH candidate AS (
                SELECT TOP (1) * FROM [ExportJobs] WITH (UPDLOCK, READPAST, READCOMMITTEDLOCK)
                WHERE [AttemptCount] < 3 AND (
                  ([State] IN (0,7) AND [AvailableAt] <= SYSUTCDATETIME() AND [LeaseToken] IS NULL)
                  OR ([State] IN (1,2,3,4) AND [LeaseExpiresAt] <= SYSUTCDATETIME()))
                ORDER BY [CreatedAt], [Id]
            )
            UPDATE candidate SET [State]=1, [Fence]=[Fence]+1, [AttemptCount]=[AttemptCount]+1,
                [LeaseOwner]=@worker, [LeaseToken]=@token,
                [LeaseExpiresAt]=DATEADD(second,@seconds,SYSUTCDATETIME()), [SafeErrorCode]=NULL
            OUTPUT INSERTED.[Id];
            """;
        command.Parameters.Add(new SqlParameter("@worker", worker));
        command.Parameters.Add(new SqlParameter("@token", token));
        command.Parameters.Add(new SqlParameter("@seconds", (int)duration.TotalSeconds));
        var claimed = await command.ExecuteScalarAsync(ct);
        if (claimed is not Guid id) { await tx.CommitAsync(ct); return null; }
        var job = await db.ExportJobs.SingleAsync(x => x.Id == id, ct);
        var now = await Now(db, ct);
        await db.ExportAttempts.Where(x => x.JobId == id && x.EndedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now).SetProperty(x => x.Outcome, "LeaseExpired"), ct);
        db.ExportAttempts.Add(new() { Id = Guid.NewGuid(), JobId = id, Number = job.AttemptCount,
            Fence = job.Fence, Worker = worker, StartedAt = now });
        db.AuditEvents.Add(Event(job, "AttemptStarted", now, job.CorrelationId));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        return new(id, worker, token, job.Fence, job.AttemptCount, job.LeaseExpiresAt!.Value);
    }

    public async Task<bool> RenewAsync(JobLease lease, TimeSpan duration, CancellationToken ct = default)
    {
        ValidateLease(lease.Worker, duration);
        await using var db = new GatewayDbContext(options);
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ExportJobs] SET [LeaseExpiresAt]=DATEADD(second,{(int)duration.TotalSeconds},SYSUTCDATETIME())
            WHERE [Id]={lease.JobId} AND [LeaseOwner]={lease.Worker} AND [LeaseToken]={lease.Token}
              AND [Fence]={lease.Fence} AND [LeaseExpiresAt]>SYSUTCDATETIME() AND [State] IN (1,2,3,4)
            """, ct);
        return changed == 1;
    }

    public async Task<bool> MoveAsync(JobLease lease, JobState from, JobState to, CancellationToken ct = default)
    {
        ValidateWorker(lease.Worker);
        // Explicit allowlist: unknown/Ready/Expired/Publishing transitions are never available here.
        if ((from, to) is not ((JobState.Preparing, JobState.Labeling) or (JobState.Labeling, JobState.Verifying)
            or (JobState.Verifying, JobState.Simulated))
            && !(from is JobState.Preparing or JobState.Labeling or JobState.Verifying or JobState.Publishing
                && to is JobState.Failed or JobState.RetryPending)) throw new InvalidOperationException("JOB_TRANSITION_INVALID");
        await using var db = new GatewayDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var terminal = to is JobState.Failed or JobState.Simulated or JobState.RetryPending;
        var changed = await db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE [ExportJobs] SET [State]={(int)to},
                [LeaseOwner]=CASE WHEN {terminal}=1 THEN NULL ELSE [LeaseOwner] END,
                [LeaseToken]=CASE WHEN {terminal}=1 THEN NULL ELSE [LeaseToken] END,
                [LeaseExpiresAt]=CASE WHEN {terminal}=1 THEN NULL ELSE [LeaseExpiresAt] END,
                [AvailableAt]=CASE WHEN {(to == JobState.RetryPending)}=1 THEN DATEADD(second,30,SYSUTCDATETIME()) ELSE [AvailableAt] END
            WHERE [Id]={lease.JobId} AND [State]={(int)from} AND [Fence]={lease.Fence}
              AND [LeaseToken]={lease.Token} AND [LeaseOwner]={lease.Worker} AND [LeaseExpiresAt]>SYSUTCDATETIME()
              AND ({(to != JobState.Simulated)}=1 OR [Mode]={(int)ExecutionMode.Simulated})
            """, ct);
        if (changed == 0) { await tx.RollbackAsync(ct); return false; }
        var now = await Now(db, ct);
        var job = await db.ExportJobs.SingleAsync(x => x.Id == lease.JobId, ct);
        if (terminal) await db.ExportAttempts.Where(x => x.JobId == job.Id && x.Fence == lease.Fence && x.EndedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now).SetProperty(x => x.Outcome, to.ToString()), ct);
        db.AuditEvents.Add(Event(job, "Job" + to, now, job.CorrelationId));
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public async Task<int> FailExhaustedAsync(CancellationToken ct = default)
    {
        await using var db = new GatewayDbContext(options);
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = await Now(db, ct);
        var jobs = await db.ExportJobs.FromSqlInterpolated($"""
            SELECT * FROM [ExportJobs] WITH (UPDLOCK,HOLDLOCK) WHERE [AttemptCount]>=3
            AND (([State] IN (1,2,3,4) AND [LeaseExpiresAt] <= {now}) OR [State]=7)
            """).ToListAsync(ct);
        foreach (var job in jobs)
        {
            job.State = JobState.Failed; job.Fence++; job.LeaseToken = null; job.LeaseOwner = null;
            job.LeaseExpiresAt = null; job.SafeErrorCode = "ATTEMPTS_EXHAUSTED";
            await db.ExportAttempts.Where(x => x.JobId == job.Id && x.EndedAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EndedAt, now).SetProperty(x => x.Outcome, "AttemptsExhausted"), ct);
            db.AuditEvents.Add(Event(job, "AttemptsExhausted", now, job.CorrelationId));
        }
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return jobs.Count;
    }

    private static void ValidateLease(string worker, TimeSpan duration)
    {
        ValidateWorker(worker);
        if (duration.TotalSeconds is < 5 or > 300)
            throw new ArgumentException("LEASE_INVALID");
    }
    private static void ValidateWorker(string worker)
    {
        if (string.IsNullOrWhiteSpace(worker) || worker.Length > 100
            || !worker.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.'))
            throw new ArgumentException("LEASE_INVALID");
    }
    private static Task<DateTimeOffset> Now(GatewayDbContext db, CancellationToken ct) => db.Database
        .SqlQueryRaw<DateTimeOffset>("SELECT TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00') AS [Value]").SingleAsync(ct);
    private static AuditEventRow Event(ExportJobRow job, string type, DateTimeOffset now, Guid correlation, Guid? actor = null)
        => new() { Id = Guid.NewGuid(), TenantId = job.TenantId, JobId = job.Id, ActorOid = actor,
            EventType = type, At = now, CorrelationId = correlation, Mode = job.Mode };
}
