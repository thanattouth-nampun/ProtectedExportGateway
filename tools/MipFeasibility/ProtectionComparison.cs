using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Gateway.Application;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;

internal sealed record ComparisonInput(string Alias,string Path,string Sha256);
internal sealed record ComparisonManifest(ComparisonInput[] Inputs);

// Operator-only readback. No SetProtection, SetLabel, Commit or decrypted-content API is called.
internal static class ProtectionComparison
{
    public static async Task<int> Run(string configPath,string manifestPath)
    {
        var locks = new List<FileStream>();
        var stage = "config";
        try
        {
            var c = JsonSerializer.Deserialize<OwnerOnlyConfig>(await System.IO.File.ReadAllTextAsync(configPath),OwnerOnlySpike.Json)
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            OwnerOnlySpike.Validate(c);
            var manifest = JsonSerializer.Deserialize<ComparisonManifest>(await System.IO.File.ReadAllTextAsync(manifestPath),OwnerOnlySpike.Json)
                ?? throw new InvalidOperationException("MANIFEST_INVALID");
            if (manifest.Inputs is not {Length:2} || manifest.Inputs[0].Alias != "sdk-copy" || manifest.Inputs[1].Alias != "excel-reference"
                || manifest.Inputs.Select(x=>x.Path).Distinct(StringComparer.OrdinalIgnoreCase).Count()!=2)
                throw new InvalidOperationException("MANIFEST_INVALID");
            foreach (var input in manifest.Inputs)
            {
                if (!Path.IsPathFullyQualified(input.Path) || !input.Path.EndsWith(".xlsx",StringComparison.OrdinalIgnoreCase)
                    || input.Sha256 is not {Length:64} || !input.Sha256.All(Uri.IsHexDigit)
                    || (System.IO.File.GetAttributes(input.Path)&FileAttributes.ReparsePoint)!=0)
                    throw new InvalidOperationException("INPUT_INVALID");
                var stream = new FileStream(input.Path,FileMode.Open,FileAccess.Read,FileShare.Read);
                locks.Add(stream);
                if (stream.Length is <=0 or >10*1024*1024 || Convert.ToHexString(SHA256.HashData(stream)) != input.Sha256)
                    throw new InvalidOperationException("INPUT_HASH_OR_SIZE_MISMATCH");
            }
            var p=c.Processor;
            stage="certificate";
            using var store = new X509Store(StoreName.My,StoreLocation.CurrentUser); store.Open(OpenFlags.ReadOnly);
            var certificates=store.Certificates.Find(X509FindType.FindByThumbprint,p.CertificateThumbprint,false);
            if (certificates.Count!=1 || !ClientCertificatePolicy.IsUsable(certificates[0],DateTimeOffset.UtcNow)) throw new InvalidOperationException("CERTIFICATE_UNAVAILABLE");
            using var certificate=certificates[0];
            var run=Path.Combine(p.PrivateRunRoot,"comparison-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(run);
            stage="native-context";
            MIP.Initialize(MipComponent.File);
            var configuration = new MipConfiguration(new ApplicationInfo { ApplicationId=p.ClientId.ToString(),
                ApplicationName="ProtectedExportGateway.ReadOnlyComparison",ApplicationVersion="0.1.0" },
                Path.Combine(run,"sdk-cache"),LogLevel.Error,false,CacheStorageType.InMemory)
            { DiagnosticOverride=new DiagnosticConfiguration { IsTraceLoggingEnabled=false,IsLocalCachingEnabled=false,IsMinimalTelemetryEnabled=true } };
            using var context=MIP.CreateMipContext(configuration);
            using var profile=await MIP.LoadFileProfileAsync(new FileProfileSettings(context,CacheStorageType.InMemory,new ScopedConsent(p.ConsentedHosts)));
            stage="engine-auth-policy";
            using var engine=await profile.AddEngineAsync(new FileEngineSettings(c.ExpectedOriginalOwner,new CertificateAuth(p,certificate),"","en-US")
            { Identity=new Identity($"{p.ClientId:D}@{p.TenantDomain}"),DelegatedUserEmail=c.ExpectedOriginalOwner });
            var age=DateTime.UtcNow-engine.LastPolicyFetchTime.ToUniversalTime();
            if (age<TimeSpan.Zero || age>TimeSpan.FromMinutes(15)) throw new InvalidOperationException("POLICY_STALE");
            var rows = new List<object>();
            foreach (var input in manifest.Inputs)
            {
                stage="inspect-"+input.Alias;
                if (!FileHandler.IsProtected(input.Path,context,null)) throw new InvalidOperationException("INPUT_NOT_PROTECTED");
                using var handler=await engine.CreateFileHandlerAsync(input.Path,input.Path,false);
                var protection=handler.Protection ?? throw new InvalidOperationException("PROTECTION_UNKNOWN");
                var descriptor=protection.ProtectionDescriptor ?? throw new InvalidOperationException("DESCRIPTOR_UNKNOWN");
                var grants=descriptor.UserRights?.Select(g=>new ObservedGrant(g.Users.ToArray(),g.Rights.ToArray())).ToArray()
                    ?? throw new InvalidOperationException("GRANTS_UNKNOWN");
                rows.Add(new { alias=input.Alias,sha256=input.Sha256,protectedState=true,
                    labelId=handler.Label?.Label?.Id,labelName=handler.Label?.Label?.Name,
                    assignmentMethod=handler.Label?.AssignmentMethod.ToString(),
                    protectionAppliedFromLabel=handler.Label?.IsProtectionAppliedFromLabel,
                    owner=protection.Owner,descriptorOwner=descriptor.Owner,contentId=descriptor.ContentId,
                    protectionType=descriptor.ProtectionType.ToString(),
                    delegatedOwnerHasOwnerRight=protection.AccessCheck(Rights.Owner),
                    userRights=grants,userRoles=descriptor.UserRoles,
                    ownerMatchesExpected=protection.Owner==c.ExpectedOriginalOwner });
            }
            for (var i=0;i<locks.Count;i++)
            {
                locks[i].Position=0;
                if (Convert.ToHexString(SHA256.HashData(locks[i]))!=manifest.Inputs[i].Sha256) throw new InvalidOperationException("INPUT_CHANGED");
            }
            var evidence=new { check="read-only-protection-comparison",status="OBSERVED_NOT_GATE_PASS",gatePassed=false,
                filesUnchanged=true,files=rows,officeAccessTested=false };
            await System.IO.File.WriteAllTextAsync(Path.Combine(run,"comparison.json"),JsonSerializer.Serialize(evidence,OwnerOnlySpike.Json));
            Console.WriteLine(JsonSerializer.Serialize(evidence,OwnerOnlySpike.Json));
            return 0; // Readback completed; never a release/security gate pass.
        }
        catch (Exception e)
        {
            var code=e is InvalidOperationException && System.Text.RegularExpressions.Regex.IsMatch(e.Message,"^[A-Z_]{3,80}$") ? e.Message : null;
            Console.WriteLine(JsonSerializer.Serialize(new { check="read-only-protection-comparison",status="FAILED",stage,code,category=e.GetType().Name,gatePassed=false }));
            return 2;
        }
        finally { foreach (var stream in locks) stream.Dispose(); }
    }
}
