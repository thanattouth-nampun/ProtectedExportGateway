using Gateway.Domain;

namespace Gateway.Infrastructure.Persistence;

// Persistence rows are internal server records, never HTTP request DTOs.
public sealed class DocumentRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OwnerOid { get; set; }
    public string Title { get; set; } = "";
    public string Purpose { get; set; } = "";
    public PublicationStatus Status { get; set; }
    public bool Enabled { get; set; } = true;
    public Guid? CurrentPublishedVersionId { get; set; }
    public long Revision { get; set; } = 1;
    public long AclRevision { get; set; } = 1;
    public byte[] RowVersion { get; set; } = [];
}
public sealed class DocumentVersionRow
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid UploaderOid { get; set; }
    public string DisplayFilename { get; set; } = "";
    public IntakeState State { get; set; }
    public string? SourceBindingJson { get; set; }
    public string? ScanEvidenceJson { get; set; }
    public string? ValidationEvidenceJson { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class DocumentAclRow
{
    public Guid DocumentId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequesterOid { get; set; }
    public bool CanRequestProtectedCopy { get; set; }
}
public sealed class ExportJobRow
{
    public Guid Id { get; set; }
    public Guid CorrelationId { get; set; }
    public Guid TenantId { get; set; }
    public Guid RequesterOid { get; set; }
    public Guid IdempotencyKey { get; set; }
    public string RequestDigest { get; set; } = "";
    public SourceKind SourceKind { get; set; }
    public ExecutionMode Mode { get; set; }
    public string SnapshotJson { get; set; } = "";
    public JobState State { get; set; } = JobState.Queued;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public long Fence { get; set; }
    public int AttemptCount { get; set; }
    public string? LeaseOwner { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public string? SafeErrorCode { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public sealed class ExportAttemptRow
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public int Number { get; set; }
    public long Fence { get; set; }
    public string Worker { get; set; } = "";
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public string? Outcome { get; set; }
}
public sealed class AuditEventRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? ActorOid { get; set; }
    public string EventType { get; set; } = "";
    public DateTimeOffset At { get; set; }
    public Guid CorrelationId { get; set; }
    public ExecutionMode Mode { get; set; }
}
