using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class OidcConfigurationTests
{
    [Fact] public async Task Code_pkce_challenge_has_only_identity_scopes_and_safe_return_path()
    {
        const string tenant="11111111-1111-1111-1111-111111111111";
        const string clientId="55555555-5555-4555-8555-555555555555";
        using var factory=new BffFactory().WithWebHostBuilder(b=>{
            b.UseSetting("WebIdentity:TenantId",tenant);
            b.UseSetting("WebIdentity:ClientId",clientId);
            b.UseSetting("WebIdentity:CertificateThumbprint","NOT-ENROLLED-HTTP-TEST");
            b.ConfigureServices(s=>s.PostConfigure<OpenIdConnectOptions>("Entra",o=> {
                o.Configuration=new OpenIdConnectConfiguration {
                    Issuer=$"https://login.microsoftonline.com/{tenant}/v2.0",
                    AuthorizationEndpoint=$"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize",
                    TokenEndpoint=$"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/token"
                };
                o.ConfigurationManager=new StaticConfigurationManager<OpenIdConnectConfiguration>(o.Configuration);
            }));
        });
        using var client=factory.CreateClient(new(){BaseAddress=new Uri("https://localhost:7188"),AllowAutoRedirect=false});
        var response=await client.GetAsync("/auth/login?returnUrl=https://example.test");
        Assert.Equal(HttpStatusCode.Redirect,response.StatusCode);
        var location=Uri.UnescapeDataString(response.Headers.Location!.ToString());
        Assert.StartsWith($"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/authorize?",location);
        Assert.Contains("response_type=code",location);Assert.Contains("code_challenge_method=S256",location);
        Assert.Contains("client_id="+clientId,location); Assert.Contains("redirect_uri=https://localhost:7188/auth/callback",location);
        Assert.DoesNotContain("offline_access",location);Assert.DoesNotContain("User.Read",location);
        Assert.DoesNotContain("client_assertion",location); Assert.DoesNotContain("client_secret",location);
        var cookies=response.Headers.GetValues("Set-Cookie").ToArray();
        Assert.All(cookies,c=>Assert.Contains("secure",c,StringComparison.OrdinalIgnoreCase));
        Assert.All(cookies,c=>Assert.Contains("httponly",c,StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(cookies,c=>c.StartsWith("__Host-gateway-session="));
    }
}
