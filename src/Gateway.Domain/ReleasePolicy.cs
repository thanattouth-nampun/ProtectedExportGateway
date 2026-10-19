using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Frozen;

namespace Gateway.Domain;

public enum ExecutionMode { Simulated, LiveMip }
public enum JobState { Queued, Preparing, Labeling, Verifying, Publishing, Ready, Failed, RetryPending, Expired, Simulated }
public enum SourceKind { ReportExport, DocumentCopy }
public sealed record PrincipalId(Guid TenantId, Guid ObjectId);
public sealed record BlobBinding(string Key, string VersionId, string ETag, string Sha256, long Length)
{
    public bool IsValid => !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(VersionId)
        && !string.IsNullOrWhiteSpace(ETag) && Sha256.Length == 64 && Sha256.All(Uri.IsHexDigit)
        && Length is > 0 and <= 10 * 1024 * 1024;
}
public sealed record RecipientMapping(PrincipalId Principal, string Address, string Version,
    DateTimeOffset ValidatedAt, bool Enabled)
{
    public bool IsFresh(PrincipalId requester, DateTimeOffset now, TimeSpan maxAge) =>
        Principal == requester && Enabled && !string.IsNullOrWhiteSpace(Version)
        && ValidatedAt <= now && now - ValidatedAt <= maxAge
        && System.Net.Mail.MailAddress.TryCreate(Address, out var mail) && mail.Address == Address
        && !Address.StartsWith("AllStaff-", StringComparison.OrdinalIgnoreCase);
}
public sealed record CopySnapshot(PrincipalId Requester, Guid DocumentId, Guid VersionId,
    BlobBinding Source, PrincipalId BusinessOwner, string LabelId, string RightsVersion,
    RecipientMapping Recipient, string PolicyVersion, string ProcessorProtectionOwner)
{
    // Canonical structured encoding avoids delimiter collisions and includes every isolation boundary.
    public string Fingerprint() => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this))));
}
public sealed record OutputObservation(string LabelId, bool? Protected, string Sha256,
    string BlobVersionId, string ETag, string[]? Recipients, string[]? Rights,
    string? ProtectionOwner, string SemanticsVersion, DateTimeOffset InspectedAt);
public static class ReleasePolicy
{
    public static readonly IReadOnlySet<string> EditSaveRights = new[] { "VIEW", "DOCEDIT", "EDIT" }.ToFrozenSet(StringComparer.Ordinal);
    public static bool CanReleaseCopy(ExecutionMode mode, CopySnapshot expected, BlobBinding artifact,
        OutputObservation observed, DateTimeOffset now, bool currentSourceAuthorized,
        bool realIntakeEvidence, bool fencedTransactionWithAudit)
    {
        return mode == ExecutionMode.LiveMip && currentSourceAuthorized && realIntakeEvidence
            && fencedTransactionWithAudit && expected.Source.IsValid && artifact.IsValid
            && !string.IsNullOrWhiteSpace(expected.LabelId) && !string.IsNullOrWhiteSpace(expected.RightsVersion)
            && !string.IsNullOrWhiteSpace(expected.PolicyVersion)
            && observed.LabelId == expected.LabelId && observed.Protected == true
            && observed.Sha256 == artifact.Sha256 && observed.BlobVersionId == artifact.VersionId
            && observed.ETag == artifact.ETag && observed.InspectedAt <= now
            && now - observed.InspectedAt <= TimeSpan.FromMinutes(15)
            && expected.Recipient.IsFresh(expected.Requester, now, TimeSpan.FromMinutes(15))
            && observed.Recipients is { Length: 1 } && observed.Recipients[0] == expected.Recipient.Address
            && observed.Rights is not null && EditSaveRights.SetEquals(observed.Rights)
            && !string.IsNullOrWhiteSpace(expected.ProcessorProtectionOwner)
            && observed.ProtectionOwner == expected.ProcessorProtectionOwner
            && observed.ProtectionOwner != expected.Recipient.Address
            && !string.IsNullOrWhiteSpace(observed.SemanticsVersion);
    }
    public static bool CanDownload(PrincipalId caller, CopySnapshot snapshot, JobState state,
        ExecutionMode mode, DateTimeOffset now, DateTimeOffset expiresAt, bool currentAuthorization,
        bool verifiedBinding, bool auditWritten) => caller == snapshot.Requester
            && mode == ExecutionMode.LiveMip && state == JobState.Ready && now < expiresAt
            && currentAuthorization && verifiedBinding && auditWritten;
}
public sealed class ExportStateMachine(ExecutionMode mode)
{
    public JobState State { get; private set; } = JobState.Queued;
    public void Move(JobState next, bool verifiedRelease = false)
    {
        var allowed = (State, next) switch
        {
            (JobState.Queued, JobState.Preparing) => true,
            (JobState.Preparing, JobState.Labeling) => true,
            (JobState.Labeling, JobState.Verifying) => true,
            (JobState.Verifying, JobState.Publishing) => mode == ExecutionMode.LiveMip,
            (JobState.Verifying, JobState.Simulated) => mode == ExecutionMode.Simulated,
            (JobState.Publishing, JobState.Ready) => mode == ExecutionMode.LiveMip && verifiedRelease,
            (JobState.Ready, JobState.Expired or JobState.Failed) => true,
            (JobState.Preparing or JobState.Labeling or JobState.Verifying or JobState.Publishing, JobState.Failed or JobState.RetryPending) => true,
            (JobState.RetryPending, JobState.Queued) => true,
            _ => false
        };
        if (!allowed) throw new InvalidOperationException("JOB_TRANSITION_INVALID");
        State = next;
    }
}
