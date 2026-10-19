using System.Security.Cryptography.X509Certificates;

namespace Gateway.Application;

public static class ClientCertificatePolicy
{
    // Entra app authentication pins the uploaded public key, not a public TLS certificate chain.
    // Thumbprint selection is performed separately; do not change TLS or trusted-root settings.
    public static bool IsUsable(X509Certificate2 certificate, DateTimeOffset now)
    {
        if (!certificate.HasPrivateKey || now.UtcDateTime < certificate.NotBefore.ToUniversalTime()
            || now.UtcDateTime >= certificate.NotAfter.ToUniversalTime()) return false;
        using var rsa = certificate.GetRSAPrivateKey();
        return rsa is not null && rsa.KeySize >= 2048;
    }
}
