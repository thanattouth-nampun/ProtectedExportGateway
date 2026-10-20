using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;

public sealed class MipAuthChallengeTests
{
    [Theory]
    [InlineData("https://syncservice.o365syncservice.com",true)]
    [InlineData("https://syncservice.o365syncservice.com/",true)]
    [InlineData("https://syncservice.o365syncservice.com/other",false)]
    [InlineData("https://syncservice.o365syncservice.com/?a=1",false)]
    [InlineData("https://syncservice.o365syncservice.com/#foo",false)]
    [InlineData("https://syncservice.o365syncservice.com.evil.test/",false)]
    public void Only_exact_root_resource_with_optional_terminal_slash_is_accepted(string value,bool expected) =>
        Assert.Equal(expected,MipAuthChallengePolicy.AcceptsResource(value,["https://syncservice.o365syncservice.com"]));
    private static readonly Guid Tenant = Guid.Parse("11111111-1111-1111-1111-111111111111");
    [Theory]
    [InlineData("https://login.windows.net/common")]
    [InlineData("https://login.microsoftonline.com/11111111-1111-1111-1111-111111111111/")]
    public void Known_discovery_or_exact_tenant_authority_is_accepted(string value) => Assert.True(MipAuthChallengePolicy.AcceptsAuthority(value,Tenant));
    [Theory]
    [InlineData("https://login.windows.net/22222222-2222-2222-2222-222222222222")]
    [InlineData("https://login.windows.net.evil.test/common")]
    [InlineData("http://login.windows.net/common")]
    [InlineData("https://login.windows.net:444/common")]
    [InlineData("https://user@login.windows.net/common")]
    [InlineData("https://login.windows.net/common?foo=bar")]
    [InlineData("https://login.windows.net/common#foo")]
    [InlineData("https://login.windows.net/organizations")]
    public void Wrong_tenant_or_untrusted_authority_rejects(string value) => Assert.False(MipAuthChallengePolicy.AcceptsAuthority(value,Tenant));
}
