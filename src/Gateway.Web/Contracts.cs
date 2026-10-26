namespace Gateway.Web;

public sealed record SessionDto(string TenantId, string ObjectId, string DisplayName, string[] Roles, string Mode, bool CanManageOwnDocuments, bool CanUploadToLocalQuarantine);
public sealed record DocumentDto(string Id, string Title, string Purpose, string Owner, string Filename,
    string Version, string Classification, string Status, bool CanRequestCopy);
public sealed record DocumentPageDto(DocumentDto[] Items, int Total);
public sealed record CsrfDto(string Token);
