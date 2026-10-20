using Gateway.Domain;

namespace Gateway.Application;

public sealed record ReviewedPortalIdentity(Guid ObjectId, string DisplayName, string RmsAddress, string ReviewReference);
public sealed class PortalIdentityDirectory
{
    public Guid TenantId { get; }
    private readonly IReadOnlyDictionary<Guid,ReviewedPortalIdentity> entries;
    public PortalIdentityDirectory(Guid tenantId,IEnumerable<ReviewedPortalIdentity> identities)
    {
        var rows=identities.ToArray();
        if (tenantId==Guid.Empty || rows.Length is <1 or >100 || rows.Select(x=>x.ObjectId).Distinct().Count()!=rows.Length
            || rows.Any(x=>x.ObjectId==Guid.Empty || string.IsNullOrWhiteSpace(x.DisplayName) || x.DisplayName.Length>200
                || string.IsNullOrWhiteSpace(x.ReviewReference) || !System.Net.Mail.MailAddress.TryCreate(x.RmsAddress,out var mail)
                || mail.Address!=x.RmsAddress || x.RmsAddress.StartsWith("AllStaff-",StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("PORTAL_DIRECTORY_INVALID");
        TenantId=tenantId; entries=rows.ToDictionary(x=>x.ObjectId);
    }
    public ReviewedPortalIdentity[] Options => entries.Values.OrderBy(x=>x.DisplayName,StringComparer.Ordinal).ToArray();
    public bool Contains(PrincipalId principal)=>principal.TenantId==TenantId && entries.ContainsKey(principal.ObjectId);
}
public sealed record OwnerActor(PrincipalId Principal,bool IsWorkspaceMember);
public sealed record DraftEdit(string Title,string Purpose,Guid[] RequesterObjectIds);
public sealed record OwnerDocumentView(Guid Id,string Title,string Purpose,long Revision,long AclRevision,string ETag,
    Guid[] RequesterObjectIds,string Status,string SourceOwnershipStatus,bool CanPublish);
public sealed record OwnerDocumentPage(OwnerDocumentView[] Items,int Total,int Skip,int PageSize);
public sealed record OwnerAuditView(string EventType,long? DocumentRevision,DateTimeOffset At,Guid CorrelationId);
public interface IOwnerWorkspace
{
    Task<OwnerDocumentPage> List(OwnerActor actor,int skip,CancellationToken ct=default);
    Task<OwnerDocumentView> Find(OwnerActor actor,Guid id,CancellationToken ct=default);
    Task<OwnerDocumentView> Create(OwnerActor actor,DraftEdit edit,Guid correlation,CancellationToken ct=default);
    Task<OwnerDocumentView> Update(OwnerActor actor,Guid id,string etag,DraftEdit edit,Guid correlation,CancellationToken ct=default);
    Task<OwnerAuditView[]> Audit(OwnerActor actor,Guid id,CancellationToken ct=default);
}
public sealed class WorkspaceFault(int status,string code):Exception(code)
{
    public int Status { get; }=status;
}
