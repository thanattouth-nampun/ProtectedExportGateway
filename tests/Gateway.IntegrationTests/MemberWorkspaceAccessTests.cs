using System.Security.Claims;
using Gateway.Web;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class MemberWorkspaceAccessTests
{
    private static readonly Guid Tenant=Guid.NewGuid();
    private static ClaimsPrincipal User(params string[] accounts) => new(new ClaimsIdentity(
        new[]{new Claim("tid",Tenant.ToString()),new Claim("oid",Guid.NewGuid().ToString())}
        .Concat(accounts.Select(x=>new Claim("acct",x))),"validated-test-cookie"));

    [Theory][InlineData("0",true)][InlineData("1",false)][InlineData("",false)][InlineData("Member",false)]
    public void Only_exact_member_claim_grants_own_workspace(string account,bool expected) =>
        Assert.Equal(expected,MemberWorkspaceAccess.IsAllowed(User(account),Tenant));

    [Fact] public void Unknown_duplicate_foreign_or_anonymous_never_grants_access()
    {
        Assert.False(MemberWorkspaceAccess.IsAllowed(User(),Tenant));
        Assert.False(MemberWorkspaceAccess.IsAllowed(User("0","0"),Tenant));
        Assert.False(MemberWorkspaceAccess.IsAllowed(User("0","1"),Tenant));
        Assert.False(MemberWorkspaceAccess.IsAllowed(User("0"),Guid.NewGuid()));
        Assert.False(MemberWorkspaceAccess.IsAllowed(User("0"),Guid.Empty));
        Assert.False(MemberWorkspaceAccess.IsAllowed(new(new ClaimsIdentity(User("0").Claims)),Tenant));
    }
    [Fact] public void Publisher_role_and_email_suffix_do_not_substitute_for_member_proof()
    {
        var user=User("1");var identity=(ClaimsIdentity)user.Identity!;
        identity.AddClaim(new("roles","DocumentPublisher"));identity.AddClaim(new("email","person@baht.net"));
        Assert.False(MemberWorkspaceAccess.IsAllowed(user,Tenant));
        identity.AddClaim(new("tid",Tenant.ToString()));
        Assert.False(MemberWorkspaceAccess.IsAllowed(user,Tenant));
    }
}
