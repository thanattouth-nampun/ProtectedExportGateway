using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Gateway.Domain;

namespace Gateway.Application;

// Constructed from trusted server records after authorization, never bound from HTTP input.
public sealed record ExportSubmission
{
    public PrincipalId Requester { get; }
    public SourceKind Kind { get; }
    public ExecutionMode Mode { get; }
    public string SnapshotJson { get; }
    public string Digest { get; }

    private ExportSubmission(PrincipalId requester, SourceKind kind, ExecutionMode mode, object snapshot)
    {
        if (requester.TenantId == Guid.Empty || requester.ObjectId == Guid.Empty || !Enum.IsDefined(mode))
            throw new ArgumentException("REQUEST_INVALID");
        Requester = requester; Kind = kind; Mode = mode;
        SnapshotJson = JsonSerializer.Serialize(new { requester, kind, mode, snapshot });
        Digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(SnapshotJson)));
    }

    public static ExportSubmission Copy(CopySnapshot snapshot, ExecutionMode mode)
    {
        if (!snapshot.Source.IsValid || snapshot.DocumentId == Guid.Empty || snapshot.VersionId == Guid.Empty
            || snapshot.Requester != snapshot.Recipient.Principal || snapshot.BusinessOwner.TenantId != snapshot.Requester.TenantId
            || string.IsNullOrWhiteSpace(snapshot.PolicyVersion) || string.IsNullOrWhiteSpace(snapshot.RightsVersion)
            || string.IsNullOrWhiteSpace(snapshot.Recipient.Version) || string.IsNullOrWhiteSpace(snapshot.LabelId)
            || string.IsNullOrWhiteSpace(snapshot.ProcessorProtectionOwner)) throw new ArgumentException("SNAPSHOT_INVALID");
        return new(snapshot.Requester, SourceKind.DocumentCopy, mode, snapshot);
    }

    public static ExportSubmission Report(PrincipalId requester, ExecutionMode mode, string reportId, string period,
        string datasetVersion, string datasetHash, string policyVersion, string rightsVersion, string labelId)
    {
        if (reportId is not ("headcount-summary" or "payroll-detail") || !DateOnly.TryParseExact(period + "-01", "yyyy-MM-dd", out _)
            || datasetHash.Length != 64 || !datasetHash.All(Uri.IsHexDigit)
            || new[] { datasetVersion, policyVersion, rightsVersion, labelId }.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("SNAPSHOT_INVALID");
        return new(requester, SourceKind.ReportExport, mode,
            new { reportId, period, datasetVersion, datasetHash, policyVersion, rightsVersion, labelId });
    }
}
