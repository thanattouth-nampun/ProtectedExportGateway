using System.Security.Cryptography.X509Certificates;
using Gateway.Application;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Web;

public static class WebClientAssertion
{
    public static string Create(Guid tenant,Guid client,string endpoint,X509Certificate2 certificate,DateTimeOffset now)
    {
        var expected=$"https://login.microsoftonline.com/{tenant:D}/oauth2/v2.0/token";
        if (tenant==Guid.Empty || client==Guid.Empty || endpoint!=expected || !ClientCertificatePolicy.IsUsable(certificate,now))
            throw new InvalidOperationException("WEB_CLIENT_CREDENTIAL_INVALID");
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor {
            Issuer=client.ToString("D"),Audience=expected,IssuedAt=now.UtcDateTime,
            NotBefore=now.AddSeconds(-30).UtcDateTime,Expires=now.AddMinutes(2).UtcDateTime,
            Claims=new Dictionary<string,object> {["sub"]=client.ToString("D"),["jti"]=Guid.NewGuid().ToString("N")},
            SigningCredentials=new X509SigningCredentials(certificate,SecurityAlgorithms.RsaSha256)
        });
    }
    public static string FromStore(Guid tenant,Guid client,string endpoint,string thumbprint)
    {
        using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser); store.Open(OpenFlags.ReadOnly);
        var found=store.Certificates.Find(X509FindType.FindByThumbprint,thumbprint,false);
        if (found.Count!=1) throw new InvalidOperationException("WEB_CLIENT_CERTIFICATE_UNAVAILABLE");
        using var certificate=found[0];
        return Create(tenant,client,endpoint,certificate,DateTimeOffset.UtcNow);
    }
}
