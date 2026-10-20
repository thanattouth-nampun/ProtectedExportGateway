using System.Security.Claims;

namespace Gateway.Web;

// Only call on a principal established by validated OIDC/server cookies. Email suffix/roles
// cannot establish Member status. Missing/duplicate/unknown acct claims fail closed.
public static class MemberWorkspaceAccess
{
    public static bool IsAllowed(ClaimsPrincipal user,Guid configuredTenant)
    {
        var tenant=user.FindAll("tid").ToArray();
        var subject=user.FindAll("oid").ToArray();
        var account=user.FindAll("acct").ToArray();
        return user.Identity?.IsAuthenticated==true && configuredTenant!=Guid.Empty
            && tenant is [var tid] && Guid.TryParse(tid.Value,out var actual) && actual==configuredTenant
            && subject is [var oid] && Guid.TryParse(oid.Value,out var id) && id!=Guid.Empty
            && account is [var acct] && acct.Value=="0";
    }
}
