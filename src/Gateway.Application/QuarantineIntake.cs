namespace Gateway.Application;

public static class QuarantineLimits
{
    public const long MaxBytes=10*1024*1024;
    public static void Validate(OwnerActor actor,Guid tenant,Guid key,string filename,bool synthetic)
    {
        if (!actor.IsWorkspaceMember || actor.Principal.TenantId!=tenant || actor.Principal.ObjectId==Guid.Empty)
            throw new WorkspaceFault(403,"INTERNAL_MEMBER_REQUIRED");
        if (!synthetic || key==Guid.Empty || string.IsNullOrWhiteSpace(filename) || filename.Length>200
            || !filename.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase)
            || filename.Any(c=>char.IsControl(c) || "<>:\"/\\|?*".Contains(c)) || filename!=filename.Trim())
            throw new WorkspaceFault(400,"UPLOAD_REQUEST_INVALID");
    }
}
// No private paths, storage tokens, source binding, scan success or eligibility in this DTO.
public sealed record QuarantineVersionView(Guid Id,string Filename,string State,long? Bytes,string? Sha256,
    DateTimeOffset UploadedAt,string? ErrorCode,string StorageMode="LocalQuarantineSpike",bool CanPublish=false);
public sealed record StoredQuarantine(Guid Token,long Bytes,string Sha256);
public interface IQuarantineObjects
{
    Task<StoredQuarantine> Write(Guid token,Stream input,CancellationToken ct);
    Task Verify(StoredQuarantine stored,CancellationToken ct);
    Task Delete(Guid token,CancellationToken ct);
    IEnumerable<(Guid Token,DateTimeOffset ModifiedAt)> Candidates(int maximum);
}
public interface IQuarantineIntake
{
    Task<QuarantineVersionView> Upload(OwnerActor actor,Guid document,string etag,Guid key,string filename,
        bool synthetic,Stream input,CancellationToken ct=default);
    Task<QuarantineVersionView[]> List(OwnerActor actor,Guid document,CancellationToken ct=default);
}
