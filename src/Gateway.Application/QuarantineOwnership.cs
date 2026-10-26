namespace Gateway.Application;

// Internal worker seam. Implementations must read a fresh SDK handler; test doubles are not live evidence.
public interface IProtectedOwnershipInspector
{
    Task<ProtectedFileInspection> Inspect(Stream encrypted,string auditFilename,string reviewedOwner,CancellationToken ct);
}

public sealed record QuarantineOwnershipReport(Guid TenantId,Guid DocumentId,Guid VersionId,Guid UploaderOid,
    string EncryptedSha256,long Bytes,string MappingReviewReference,string Outcome)
{
    public bool CanPublish => false;
    public bool GatePassed => false;
}

public static class QuarantineOwnershipRules
{
    public static string Evaluate(ProtectedFileInspection observed,string approvedLabel,string reviewedOwner)
    {
        if(observed.IsProtected!=true || string.IsNullOrWhiteSpace(observed.LabelId)
            || string.IsNullOrWhiteSpace(observed.Owner) || observed.Grants is null || observed.HasRoleGrants is null)
            return "PROTECTION_UNKNOWN_OR_UNSUPPORTED";
        if(!string.Equals(observed.Owner,reviewedOwner,StringComparison.Ordinal)) return "RMS_OWNER_MISMATCH";
        return ProtectedSourceRules.IsOwnerOnly(observed,approvedLabel,reviewedOwner)
            ? "OWNER_ONLY_OBSERVED_NOT_ELIGIBLE" : "LABEL_OR_OWNER_ONLY_GRANTS_REJECTED";
    }
}
