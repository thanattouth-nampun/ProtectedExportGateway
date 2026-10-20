using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Gateway.Web;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Gateway.IntegrationTests;

public sealed class WebClientAssertionTests
{
    [Fact] public async Task Short_lived_assertion_is_signed_and_bound_to_one_client_and_tenant_token_endpoint()
    {
        using var key=RSA.Create(2048);
        var now=DateTimeOffset.UtcNow;
        using var cert=new CertificateRequest("CN=ephemeral-web-test",key,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1).CreateSelfSigned(now.AddMinutes(-1),now.AddHours(1));
        var tenant=Guid.NewGuid(); var client=Guid.NewGuid();var endpoint=$"https://login.microsoftonline.com/{tenant:D}/oauth2/v2.0/token";
        var token=WebClientAssertion.Create(tenant,client,endpoint,cert,now);
        var handler=new JsonWebTokenHandler();
        var validation=new TokenValidationParameters {ValidIssuer=client.ToString("D"),ValidAudience=endpoint,
            IssuerSigningKey=new X509SecurityKey(cert),ValidateIssuerSigningKey=true,ValidateLifetime=true,ClockSkew=TimeSpan.Zero};
        Assert.True((await handler.ValidateTokenAsync(token,validation)).IsValid);
        var claims=handler.ReadJsonWebToken(token);
        Assert.Equal(client.ToString("D"),claims.Subject); Assert.InRange(claims.ValidTo-now.UtcDateTime,TimeSpan.FromSeconds(118),TimeSpan.FromSeconds(121));
        var second=handler.ReadJsonWebToken(WebClientAssertion.Create(tenant,client,endpoint,cert,now));
        Assert.NotEqual(claims.Id,second.Id);
        validation.ValidAudience="https://example.test/token";
        Assert.False((await handler.ValidateTokenAsync(token,validation)).IsValid);
        Assert.Throws<InvalidOperationException>(()=>WebClientAssertion.Create(tenant,client,"https://example.test/token",cert,now));
    }
}
