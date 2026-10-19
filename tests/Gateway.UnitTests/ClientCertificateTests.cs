using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;

public sealed class ClientCertificateTests
{
    [Fact] public void In_memory_self_signed_key_is_usable_without_enrolling_or_trusting_it()
    {
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(2048);
        using var cert = new CertificateRequest("CN=EphemeralUnitFixture",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1)
            .CreateSelfSigned(now.AddMinutes(-1),now.AddDays(1));
        Assert.True(ClientCertificatePolicy.IsUsable(cert,now));
    }
    [Theory]
    [InlineData("expired")][InlineData("future")][InlineData("weak")]
    public void Expired_future_or_weak_keys_are_rejected(string fault)
    {
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(fault=="weak" ? 1024 : 2048);
        var start = fault=="future" ? now.AddDays(1) : now.AddDays(-2);
        var end = fault=="expired" ? now.AddDays(-1) : now.AddDays(2);
        using var cert = new CertificateRequest("CN=EphemeralUnitFixture",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1).CreateSelfSigned(start,end);
        Assert.False(ClientCertificatePolicy.IsUsable(cert,now));
    }
    [Fact] public void Public_certificate_without_private_key_is_rejected()
    {
        var now = DateTimeOffset.UtcNow;
        using var rsa = RSA.Create(2048);
        using var cert = new CertificateRequest("CN=EphemeralUnitFixture",rsa,HashAlgorithmName.SHA256,RSASignaturePadding.Pkcs1).CreateSelfSigned(now.AddMinutes(-1),now.AddDays(1));
        using var publicOnly = X509CertificateLoader.LoadCertificate(cert.RawData);
        Assert.False(ClientCertificatePolicy.IsUsable(publicOnly,now));
    }
    [Fact] public void Non_rsa_certificate_is_rejected()
    {
        var now = DateTimeOffset.UtcNow;
        using var key = ECDsa.Create();
        using var cert = new CertificateRequest("CN=EphemeralUnitFixture",key,HashAlgorithmName.SHA256).CreateSelfSigned(now.AddMinutes(-1),now.AddDays(1));
        Assert.False(ClientCertificatePolicy.IsUsable(cert,now));
    }
}
