namespace Gateway.Application;

public static class MipAuthChallengePolicy
{
    public static bool AcceptsResource(string resource, IEnumerable<string> allowed) =>
        Uri.TryCreate(resource,UriKind.Absolute,out var uri) && uri.Scheme == "https" && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo) && uri.Query.Length == 0 && uri.Fragment.Length == 0
        && uri.AbsolutePath == "/" && allowed.Any(value => string.Equals(value.TrimEnd('/'),resource.TrimEnd('/'),StringComparison.Ordinal));

    // A common challenge is discovery only. The caller must request tokens at its fixed configured tenant.
    public static bool AcceptsAuthority(string authority, Guid tenant) => tenant != Guid.Empty
        && Uri.TryCreate(authority,UriKind.Absolute,out var uri) && uri.Scheme == "https" && uri.IsDefaultPort
        && string.IsNullOrEmpty(uri.UserInfo) && uri.Query.Length == 0 && uri.Fragment.Length == 0
        && (uri.Host == "login.microsoftonline.com" || uri.Host == "login.windows.net")
        && (uri.AbsolutePath.Trim('/') == tenant.ToString("D") || uri.AbsolutePath.Trim('/') == "common");
}
