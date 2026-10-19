using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.Exceptions;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;

internal sealed record SpikeConfig(Guid TenantId, Guid ClientId, string TenantDomain, string CertificateThumbprint,
    string InputPath, string PrivateRunRoot, string LabelOnlyId, string ConfidentialId, string UdpId,
    string RecipientA, string RecipientB, string ExpectedProtectionOwner, string[] AllowedResources, string[] ConsentedHosts,
    bool OperatorAcknowledgedSdkNetworkAndDiagnostics);

internal static class LiveSpike
{
    public static async Task<int> Run(string configPath)
    {
        try
        {
            var c = JsonSerializer.Deserialize<SpikeConfig>(await File.ReadAllTextAsync(configPath))
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            if (c.TenantId == Guid.Empty || c.ClientId == Guid.Empty || string.IsNullOrWhiteSpace(c.TenantDomain)
                || Uri.CheckHostName(c.TenantDomain) != UriHostNameType.Dns || !c.OperatorAcknowledgedSdkNetworkAndDiagnostics
                || !Path.IsPathFullyQualified(c.PrivateRunRoot) || !Path.IsPathFullyQualified(c.InputPath)
                || !File.Exists(c.InputPath) || new FileInfo(c.InputPath).Length > 10 * 1024 * 1024
                || !c.InputPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase)
                || c.RecipientA == c.RecipientB || !IsRecipient(c.RecipientA) || !IsRecipient(c.RecipientB)
                || string.IsNullOrWhiteSpace(c.ExpectedProtectionOwner) || c.ExpectedProtectionOwner == c.RecipientA || c.ExpectedProtectionOwner == c.RecipientB
                || c.AllowedResources.Length == 0 || c.ConsentedHosts.Length == 0
                || new[] { c.LabelOnlyId, c.ConfidentialId, c.UdpId }.Any(s => !Guid.TryParse(s, out _)))
                throw new InvalidOperationException("CONFIG_INVALID");
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            var certs = store.Certificates.Find(X509FindType.FindByThumbprint, c.CertificateThumbprint, validOnly: true);
            if (certs.Count != 1 || !certs[0].HasPrivateKey) throw new InvalidOperationException("CERTIFICATE_UNAVAILABLE");
            using var cert = certs[0];
            var run = Path.Combine(c.PrivateRunRoot, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(run); // Operator must provision a private ACL on the parent first.
            MIP.Initialize(MipComponent.File);
            var mipConfig = new MipConfiguration(new ApplicationInfo
            { ApplicationId = c.ClientId.ToString(), ApplicationName = "ProtectedExportGateway.Feasibility", ApplicationVersion = "0.1.0" },
                Path.Combine(run, "sdk-cache"), Microsoft.InformationProtection.LogLevel.Error, false, CacheStorageType.InMemory)
            {
                DiagnosticOverride = new DiagnosticConfiguration
                { IsTraceLoggingEnabled = false, IsLocalCachingEnabled = false, IsMinimalTelemetryEnabled = true }
            };
            using var context = MIP.CreateMipContext(mipConfig);
            using var profile = await MIP.LoadFileProfileAsync(new FileProfileSettings(context, CacheStorageType.InMemory, new ScopedConsent(c.ConsentedHosts)));
            var auth = new CertificateAuth(c, cert);
            var settings = new FileEngineSettings("", auth, "", "en-US")
            {
                // Service principal identity, not a delegated downloader/user policy identity.
                Identity = new Identity($"{c.ClientId:D}@{c.TenantDomain}"), ProtectionOnlyEngine = false
            };
            using var engine = await profile.AddEngineAsync(settings);
            var age = DateTime.UtcNow - engine.LastPolicyFetchTime.ToUniversalTime();
            if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(15)) throw new InvalidOperationException("POLICY_STALE");
            if (FileHandler.IsProtected(c.InputPath, context, null)) throw new InvalidOperationException("PROTECTED_SOURCE_REJECTED");
            var results = new List<object>();
            var allMatch = true;
            foreach (var scenario in new[] {
                (Alias: "summary", Label: c.LabelOnlyId, Encrypted: false, Recipient: (string?)null),
                (Alias: "payroll", Label: c.ConfidentialId, Encrypted: true, Recipient: (string?)null),
                (Alias: "copy-a", Label: c.UdpId, Encrypted: true, Recipient: (string?)c.RecipientA),
                (Alias: "copy-b", Label: c.UdpId, Encrypted: true, Recipient: (string?)c.RecipientB) })
            {
                var label = engine.GetLabelById(scenario.Label);
                if (label is null || !label.IsActive || (scenario.Recipient is not null && !label.HasAdhocProtection))
                    throw new InvalidOperationException("LABEL_UNAVAILABLE_OR_NOT_UDP");
                var output = Path.Combine(run, scenario.Alias + ".xlsx");
                using (var writer = await engine.CreateFileHandlerAsync(c.InputPath, c.InputPath, false))
                {
                    var options = new LabelingOptions { AssignmentMethod = AssignmentMethod.Standard };
                    if (scenario.Recipient is not null)
                    {
                        // Fresh descriptor per copy. Never reuse a recipient collection.
                        writer.SetProtection(new ProtectionDescriptor(new List<UserRights>
                        { new([scenario.Recipient], [Rights.View, Rights.DocumentEdit, Rights.Edit]) }), new ProtectionSettings());
                    }
                    writer.SetLabel(label, options, new ProtectionSettings());
                    if (!await writer.CommitAsync(output)) throw new InvalidOperationException("COMMIT_FAILED");
                }
                using var reader = await engine.CreateFileHandlerAsync(output, output, false);
                var protectedState = FileHandler.IsProtected(output, context, null);
                var observedLabel = reader.Label?.Label?.Id;
                var observedProtection = reader.Protection;
                var descriptor = observedProtection?.ProtectionDescriptor;
                var observedGrants = descriptor?.UserRights;
                var observedRecipients = observedGrants?.SelectMany(g => g.Users).Distinct(StringComparer.Ordinal).Order().ToArray();
                var observedRights = observedGrants?.SelectMany(g => g.Rights).Distinct(StringComparer.Ordinal).Order().ToArray();
                var rightsMatch = scenario.Recipient is null || observedGrants is { Count: 1 }
                    && observedRecipients is { Length: 1 } && observedRecipients[0] == scenario.Recipient
                    && observedRights is not null && new HashSet<string>([Rights.View, Rights.DocumentEdit, Rights.Edit]).SetEquals(observedRights)
                    && (descriptor?.UserRoles is null || descriptor.UserRoles.Count == 0)
                    && !string.IsNullOrWhiteSpace(observedProtection?.Owner) && observedProtection.Owner != scenario.Recipient;
                var ownerMatch = !scenario.Encrypted || observedProtection?.Owner == c.ExpectedProtectionOwner;
                var match = observedLabel == scenario.Label && protectedState == scenario.Encrypted && rightsMatch && ownerMatch;
                allMatch &= match;
                results.Add(new { scenario.Alias, matched = match, observedLabelMatch = observedLabel == scenario.Label,
                    observedProtection = protectedState, independentRecipientReadback = observedGrants is not null,
                    exactRecipientAndRightsMatch = rightsMatch, outputSha256 = Hash(output),
                    observedRights, protectionOwnerMatchesExpected = ownerMatch,
                    semantics = "FileSDK-1.18.124/fresh-handler/ProtectionDescriptor.UserRights" });
            }
            var evidence = new { gate = "G1", status = "PARTIAL_NOT_PASSED", applyInspectMatched = allMatch,
                sdk = "1.18.124", inputSha256 = Hash(c.InputPath), results,
                untested = new[] { "negative configuration cases", "cold/warm cache and refresh", "G2 isolated Office edit/save/cross-open", "Blob publication" } };
            await File.WriteAllTextAsync(Path.Combine(run, "observations.json"), JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine(JsonSerializer.Serialize(evidence));
            return allMatch ? 3 : 2; // Even matching initial observations do not pass the complete G1 gate.
        }
        catch (Exception e)
        {
            // No raw native/MSAL exception messages or tenant/config values in normal output.
            Console.WriteLine(JsonSerializer.Serialize(new { gate = "G1", status = "BLOCKED_OR_FAILED", category = e.GetType().Name }));
            return 2;
        }
    }
    private static bool IsRecipient(string value) => System.Net.Mail.MailAddress.TryCreate(value, out var mail)
        && mail.Address == value && !value.StartsWith("AllStaff-", StringComparison.OrdinalIgnoreCase);
    private static string Hash(string file) { using var stream = File.OpenRead(file); return Convert.ToHexString(SHA256.HashData(stream)); }
}
internal sealed class ScopedConsent(string[] hosts) : IConsentDelegate
{
    public Consent GetUserConsent(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme == "https" && uri.IsDefaultPort && string.IsNullOrEmpty(uri.UserInfo)
        && hosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase) ? Consent.Accept : Consent.Reject;
}
internal sealed class CertificateAuth : IAuthDelegate
{
    private readonly SpikeConfig config;
    private readonly IConfidentialClientApplication client;
    public CertificateAuth(SpikeConfig config, X509Certificate2 certificate)
    {
        this.config = config;
        client = ConfidentialClientApplicationBuilder.Create(config.ClientId.ToString())
            .WithAuthority($"https://login.microsoftonline.com/{config.TenantId:D}")
            .WithCertificate(certificate).Build();
    }
    public string AcquireToken(Identity identity, string authority, string resource, string claims)
    {
        if (!Uri.TryCreate(authority, UriKind.Absolute, out var uri) || uri.Scheme != "https"
            || uri.Host != "login.microsoftonline.com" || !uri.IsDefaultPort || uri.Query.Length != 0
            || !string.IsNullOrEmpty(uri.UserInfo) || uri.AbsolutePath.Trim('/') != config.TenantId.ToString("D")
            || !config.AllowedResources.Contains(resource, StringComparer.Ordinal)
            || !Uri.TryCreate(resource, UriKind.Absolute, out var res) || res.Scheme != "https"
            || !res.IsDefaultPort || !string.IsNullOrEmpty(res.UserInfo) || res.Query.Length != 0)
            throw new InvalidOperationException("AUTH_CHALLENGE_NOT_ALLOWLISTED");
        var request = client.AcquireTokenForClient([resource.TrimEnd('/') + "/.default"]);
        if (!string.IsNullOrEmpty(claims)) request = request.WithClaims(claims);
        return request.ExecuteAsync().GetAwaiter().GetResult().AccessToken;
    }
}
