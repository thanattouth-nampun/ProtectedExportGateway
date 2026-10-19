using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Gateway.Application;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;

// Private operator configuration, never an HTTP DTO. Processor retains the existing certificate/allowlist contract.
internal sealed record OwnerOnlyConfig(SpikeConfig Processor, string ExpectedInputSha256, string ExpectedOriginalOwner,
    string AuthorizationReference, bool SyntheticFixtureAcknowledged, bool OwnerDelegationApproved);

internal static class OwnerOnlySpike
{
    internal static readonly JsonSerializerOptions Json = new() { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, WriteIndented = true };
    public static async Task<int> Run(string path, bool execute)
    {
        try
        {
            var c = JsonSerializer.Deserialize<OwnerOnlyConfig>(await System.IO.File.ReadAllTextAsync(path),Json)
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            Validate(c);
            var p = c.Processor;
            // Lock the source against ordinary write/delete for the whole run; pin the supplied encrypted bytes.
            using var sourceLock = new FileStream(p.InputPath,FileMode.Open,FileAccess.Read,FileShare.Read);
            var sourceHash = Convert.ToHexString(SHA256.HashData(sourceLock));
            if (sourceLock.Length is <= 0 or > 10*1024*1024 || sourceHash != c.ExpectedInputSha256)
                throw new InvalidOperationException("SOURCE_HASH_OR_SIZE_MISMATCH");
            if (!execute)
            {
                Console.WriteLine(JsonSerializer.Serialize(new { check="owner-only-preflight",status="LOCAL_CONFIG_ONLY",
                    inputSha256=sourceHash,liveAuthentication="NOT_RUN",protectionInspection="NOT_RUN",gatePassed=false }));
                return 0;
            }
            using var store = new X509Store(StoreName.My,StoreLocation.CurrentUser); store.Open(OpenFlags.ReadOnly);
            var certificates = store.Certificates.Find(X509FindType.FindByThumbprint,p.CertificateThumbprint,false);
            if (certificates.Count != 1 || !ClientCertificatePolicy.IsUsable(certificates[0],DateTimeOffset.UtcNow)) throw new InvalidOperationException("CERTIFICATE_UNAVAILABLE");
            using var certificate = certificates[0];
            var run = Path.Combine(p.PrivateRunRoot,"owner-only-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(run);
            MIP.Initialize(MipComponent.File);
            var configuration = new MipConfiguration(new ApplicationInfo { ApplicationId=p.ClientId.ToString(),
                ApplicationName="ProtectedExportGateway.OwnerOnlySpike",ApplicationVersion="0.2.0" },
                Path.Combine(run,"sdk-cache"),LogLevel.Error,false,CacheStorageType.InMemory)
            { DiagnosticOverride=new DiagnosticConfiguration { IsTraceLoggingEnabled=false,IsLocalCachingEnabled=false,IsMinimalTelemetryEnabled=true } };
            using var context = MIP.CreateMipContext(configuration);
            if (!FileHandler.IsProtected(p.InputPath,context,null)) throw new InvalidOperationException("SOURCE_NOT_PROTECTED");
            using var profile = await MIP.LoadFileProfileAsync(new FileProfileSettings(context,CacheStorageType.InMemory,new ScopedConsent(p.ConsentedHosts)));
            // Explicit candidate: certificate app-only authentication, engine delegation to the original owner.
            // Setting this address requests delegation; it does NOT grant or prove the service's authorization.
            var settings = new FileEngineSettings(c.ExpectedOriginalOwner,new CertificateAuth(p,certificate),"","en-US")
            { Identity=new Identity($"{p.ClientId:D}@{p.TenantDomain}"),DelegatedUserEmail=c.ExpectedOriginalOwner };
            using var engine = await profile.AddEngineAsync(settings);
            var age = DateTime.UtcNow-engine.LastPolicyFetchTime.ToUniversalTime();
            if (age < TimeSpan.Zero || age > TimeSpan.FromMinutes(15)) throw new InvalidOperationException("POLICY_STALE");
            var label = engine.GetLabelById(p.UdpId);
            if (label is null || !label.IsActive || !label.HasAdhocProtection) throw new InvalidOperationException("LABEL_NOT_ACTIVE_UDP");
            string? sourceContentId;
            using (var inspector = await engine.CreateFileHandlerAsync(p.InputPath,p.InputPath,false))
            {
                if (!ProtectedSourceRules.IsOwnerOnly(Inspect(inspector,true),p.UdpId,c.ExpectedOriginalOwner))
                    throw new InvalidOperationException("SOURCE_NOT_OBSERVED_OWNER_ONLY");
                sourceContentId = inspector.Protection?.ProtectionDescriptor?.ContentId;
                if (string.IsNullOrWhiteSpace(sourceContentId)) throw new InvalidOperationException("SOURCE_CONTENT_ID_UNKNOWN");
            }
            var results = new List<object>(); var allMatched = true;
            var contentIds = new HashSet<string>(StringComparer.Ordinal) { sourceContentId };
            foreach (var scenario in new[] { (Alias:"copy-a",Recipient:p.RecipientA),(Alias:"copy-b",Recipient:p.RecipientB) })
            {
                var output = Path.Combine(run,scenario.Alias+".xlsx");
                using (var writer = await engine.CreateFileHandlerAsync(p.InputPath,p.InputPath,false))
                {
                    if (writer.Protection is null || !(writer.Protection.AccessCheck(Rights.Owner) || writer.Protection.AccessCheck(Rights.Export)))
                        throw new InvalidOperationException("SOURCE_REPROTECTION_NOT_AUTHORIZED");
                    writer.SetProtection(new ProtectionDescriptor(new List<UserRights> {
                        new([scenario.Recipient],[Rights.View,Rights.DocumentEdit,Rights.Edit]) }),
                        new ProtectionSettings { DelegatedUserEmail=c.ExpectedOriginalOwner });
                    // Preserve label by applying the same ID; no global policy/label mutation, no protection removal.
                    writer.SetLabel(label,new LabelingOptions { AssignmentMethod=AssignmentMethod.Standard },
                        new ProtectionSettings { DelegatedUserEmail=c.ExpectedOriginalOwner });
                    if (!await writer.CommitAsync(output)) throw new InvalidOperationException("COMMIT_FAILED");
                }
                using var outputLock = new FileStream(output,FileMode.Open,FileAccess.Read,FileShare.Read);
                if (outputLock.Length is <=0 or >10*1024*1024) throw new InvalidOperationException("OUTPUT_SIZE_INVALID");
                using var reader = await engine.CreateFileHandlerAsync(output,output,false);
                var outputContentId = reader.Protection?.ProtectionDescriptor?.ContentId;
                var distinctContentId = !string.IsNullOrWhiteSpace(outputContentId) && contentIds.Add(outputContentId);
                var observed = Inspect(reader,FileHandler.IsProtected(output,context,null));
                var matched = ProtectedSourceRules.IsRecipientCopy(observed,
                    p.UdpId,c.ExpectedOriginalOwner,scenario.Recipient) && distinctContentId;
                allMatched &= matched;
                results.Add(new { scenario.Alias,matched,outputSha256=Convert.ToHexString(SHA256.HashData(outputLock)),
                    independentHandler=true,distinctContentId,observedProtection=observed.IsProtected,
                    observedLabelMatches=observed.LabelId==p.UdpId,observedOwnerMatches=observed.Owner==c.ExpectedOriginalOwner,
                    observedUserGrantCount=observed.Grants?.Length,observed.HasRoleGrants,
                    observedRights=observed.Grants?.SelectMany(g=>g.Rights).Distinct(StringComparer.Ordinal).Order().ToArray(),
                    recipientMatches=observed.Grants is [{Users:[var user]}] && user==scenario.Recipient,
                    semantics="FileSDK-1.18.124/fresh-read-locked-handler/ProtectionDescriptor.UserRights" });
            }
            if (Hash(p.InputPath) != sourceHash) throw new InvalidOperationException("SOURCE_CHANGED");
            var evidence = new { gate="G1-owner-only",status="PARTIAL_NOT_PASSED",gatePassed=false,
                identityCandidate="certificate-app-only/original-owner-engine-delegation",inputSha256=sourceHash,
                sourceUnchanged=true,applyInspectMatched=allMatched,results,
                pending=new[] {"Independent owner/A/B/unauthorized-C Office tests, including saved copies","Cold/warm policy and access-negative controls",
                    "G6 encrypted-source content scan and bounded structural validation","Exact Blob version binding and fenced release"} };
            await System.IO.File.WriteAllTextAsync(Path.Combine(run,"observations.json"),JsonSerializer.Serialize(evidence,Json));
            Console.WriteLine(JsonSerializer.Serialize(evidence,Json)); return allMatched ? 3 : 2;
        }
        catch (Exception e)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { gate="G1-owner-only",status="BLOCKED_OR_FAILED",category=e.GetType().Name,gatePassed=false }));
            return 2;
        }
    }
    private static ProtectedFileInspection Inspect(IFileHandler handler, bool protectedState)
    {
        var descriptor = handler.Protection?.ProtectionDescriptor;
        var grants = descriptor?.UserRights?.Select(g => new ObservedGrant(g.Users.ToArray(),g.Rights.ToArray())).ToArray();
        return new(protectedState,handler.Label?.Label?.Id,handler.Protection?.Owner,grants,
            descriptor is null ? null : descriptor.UserRoles is {Count:>0});
    }
    private static void Validate(OwnerOnlyConfig c)
    {
        var p = c.Processor;
        if (p is null || !c.SyntheticFixtureAcknowledged || !c.OwnerDelegationApproved
            || string.IsNullOrWhiteSpace(c.AuthorizationReference) || !p.OperatorAcknowledgedSdkNetworkAndDiagnostics
            || p.TenantId==Guid.Empty || p.ClientId==Guid.Empty || Uri.CheckHostName(p.TenantDomain)!=UriHostNameType.Dns
            || !Guid.TryParse(p.UdpId,out _) || !Mail(c.ExpectedOriginalOwner) || !Mail(p.RecipientA) || !Mail(p.RecipientB)
            || new[] {c.ExpectedOriginalOwner,p.RecipientA,p.RecipientB}.Distinct(StringComparer.OrdinalIgnoreCase).Count()!=3
            || p.ExpectedProtectionOwner != c.ExpectedOriginalOwner || string.IsNullOrWhiteSpace(p.CertificateThumbprint)
            || p.AllowedResources is not {Length:>0} || p.ConsentedHosts is not {Length:>0}
            || !Path.IsPathFullyQualified(p.InputPath) || !p.InputPath.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase)
            || !Path.IsPathFullyQualified(p.PrivateRunRoot) || !Directory.Exists(p.PrivateRunRoot)
            || c.ExpectedInputSha256 is not {Length:64} || !c.ExpectedInputSha256.All(Uri.IsHexDigit))
            throw new InvalidOperationException("CONFIG_INVALID");
        var root = Path.GetFullPath(p.PrivateRunRoot);
        var input = Path.GetFullPath(p.InputPath);
        if (input.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)
            || root.Contains("wwwroot",StringComparison.OrdinalIgnoreCase)
            || (System.IO.File.GetAttributes(root)&FileAttributes.ReparsePoint)!=0
            || (System.IO.File.GetAttributes(input)&FileAttributes.ReparsePoint)!=0)
            throw new InvalidOperationException("PRIVATE_SOURCE_AND_RUN_ROOT_REQUIRED");
    }
    private static bool Mail(string value) => !string.IsNullOrWhiteSpace(value)
        && System.Net.Mail.MailAddress.TryCreate(value,out var mail) && mail.Address==value
        && !value.StartsWith("AllStaff-",StringComparison.OrdinalIgnoreCase);
    private static string Hash(string path) { using var stream=System.IO.File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(stream)); }
}
