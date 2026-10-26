using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Gateway.Application;
using Gateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;

internal sealed record QuarantineProbeConfig(string ConnectionString,string PrivateRoot,Guid TenantId,Guid VersionId,
    string EncryptedSha256,string ApprovedLabelId,ReviewedPortalIdentity[] Identities,
    string AuthorizationReference,bool SyntheticFixtureAcknowledged);

internal static class QuarantineOwnerProbe
{
    public static async Task<int> Run(string configPath,string? authPath)
    {
        var stage="config";
        try
        {
            var c=JsonSerializer.Deserialize<QuarantineProbeConfig>(await System.IO.File.ReadAllTextAsync(configPath),OwnerOnlySpike.Json)
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            if(!c.SyntheticFixtureAcknowledged || string.IsNullOrWhiteSpace(c.AuthorizationReference)
                || string.IsNullOrWhiteSpace(c.ConnectionString) || !Path.IsPathFullyQualified(c.PrivateRoot))
                throw new InvalidOperationException("CONFIG_INVALID");
            // This diagnostic is limited to Windows LocalDB, not a cloud deployment or general SQL connector.
            var sql=new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(c.ConnectionString);
            if(!sql.IntegratedSecurity || !sql.DataSource.Equals("(localdb)\\MSSQLLocalDB",StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("LOCALDB_ONLY");
            var directory=new PortalIdentityDirectory(c.TenantId,c.Identities);
            var objects=new LocalQuarantineObjects(c.PrivateRoot);
            var options=new DbContextOptionsBuilder<GatewayDbContext>().UseSqlServer(c.ConnectionString,
                s=>s.UseCompatibilityLevel(160)).Options;
            IProtectedOwnershipInspector? inspector=null;
            if(authPath is not null)
            {
                var auth=JsonSerializer.Deserialize<OwnerOnlyConfig>(await System.IO.File.ReadAllTextAsync(authPath),OwnerOnlySpike.Json)
                    ?? throw new InvalidOperationException("CONFIG_INVALID");
                OwnerOnlySpike.Validate(auth);
                if(auth.Processor.TenantId!=c.TenantId || auth.ExpectedInputSha256!=c.EncryptedSha256
                    || auth.Processor.UdpId!=c.ApprovedLabelId || auth.AuthorizationReference!=c.AuthorizationReference)
                    throw new InvalidOperationException("AUTH_TARGET_MISMATCH");
                inspector=new SdkQuarantineOwnershipInspector(auth);
            }
            stage="locked-receipt-inspection";
            var report=await new SqlQuarantineOwnershipProbe(options,directory,objects).Run(c.TenantId,c.VersionId,
                c.EncryptedSha256,c.ApprovedLabelId,inspector);
            Console.WriteLine(JsonSerializer.Serialize(new {check="quarantine-ownership",report,
                mode=inspector is null ? "LOCAL_PREFLIGHT_ONLY" : "SDK_READBACK_DIAGNOSTIC",
                scan="NOT_RUN",contentValidation="NOT_RUN",databaseWrites=false,gatePassed=false},OwnerOnlySpike.Json));
            return report.Outcome is "LOCAL_PREFLIGHT_ONLY_INSPECTION_NOT_RUN" or "OWNER_ONLY_OBSERVED_NOT_ELIGIBLE" ? 0 : 2;
        }
        catch(Exception e)
        {
            var code=e is InvalidOperationException && System.Text.RegularExpressions.Regex.IsMatch(e.Message,"^[A-Z_]{3,80}$") ? e.Message : null;
            Console.WriteLine(JsonSerializer.Serialize(new {check="quarantine-ownership",status="FAILED",stage,code,
                category=e.GetType().Name,gatePassed=false}));
            return 2;
        }
    }
}

// Readback only: no SetLabel/SetProtection/Commit/decrypted-temporary-content API.
internal sealed class SdkQuarantineOwnershipInspector(OwnerOnlyConfig config):IProtectedOwnershipInspector
{
    public async Task<ProtectedFileInspection> Inspect(Stream encrypted,string auditFilename,string reviewedOwner,CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        // Delegation is separately scoped by the operator's hash-pinned original-owner authorization.
        // Never request owner delegation to the uploader merely because they uploaded these bytes.
        var p=config.Processor;
        using var store=new X509Store(StoreName.My,StoreLocation.CurrentUser);store.Open(OpenFlags.ReadOnly);
        var matches=store.Certificates.Find(X509FindType.FindByThumbprint,p.CertificateThumbprint,false);
        if(matches.Count!=1 || !ClientCertificatePolicy.IsUsable(matches[0],DateTimeOffset.UtcNow))
            throw new InvalidOperationException("CERTIFICATE_UNAVAILABLE");
        using var certificate=matches[0];
        // Reuse reviewed private-ACL/reparse checks. SDK cache still has the same-user host trust limit.
        _=new LocalQuarantineObjects(p.PrivateRunRoot);
        MIP.Initialize(MipComponent.File);
        var configuration=new MipConfiguration(new ApplicationInfo {ApplicationId=p.ClientId.ToString(),
            ApplicationName="ProtectedExportGateway.QuarantineOwnerReadback",ApplicationVersion="0.1.0"},
            Path.Combine(p.PrivateRunRoot,"quarantine-owner-"+Guid.NewGuid().ToString("N")),LogLevel.Error,false,CacheStorageType.InMemory)
        {DiagnosticOverride=new DiagnosticConfiguration {IsTraceLoggingEnabled=false,IsLocalCachingEnabled=false,IsMinimalTelemetryEnabled=true}};
        using var context=MIP.CreateMipContext(configuration);
        var protectedState=FileHandler.IsProtected(encrypted,auditFilename,context,null);encrypted.Position=0;
        if(!protectedState) return new(false,null,null,null,null);
        using var profile=await MIP.LoadFileProfileAsync(new FileProfileSettings(context,CacheStorageType.InMemory,new ScopedConsent(p.ConsentedHosts)));
        using var engine=await profile.AddEngineAsync(new FileEngineSettings(config.ExpectedOriginalOwner,new CertificateAuth(p,certificate),"","en-US")
        {Identity=new Identity($"{p.ClientId:D}@{p.TenantDomain}"),DelegatedUserEmail=config.ExpectedOriginalOwner});
        var age=DateTime.UtcNow-engine.LastPolicyFetchTime.ToUniversalTime();
        if(age<TimeSpan.Zero || age>TimeSpan.FromMinutes(15)) throw new InvalidOperationException("POLICY_STALE");
        var label=engine.GetLabelById(p.UdpId);
        if(label is null || !label.IsActive || !label.HasAdhocProtection) throw new InvalidOperationException("LABEL_NOT_ACTIVE_UDP");
        using var handler=await engine.CreateFileHandlerAsync(encrypted,auditFilename,false);
        var descriptor=handler.Protection?.ProtectionDescriptor;
        if(descriptor is not null && descriptor.Owner!=handler.Protection?.Owner)
            throw new InvalidOperationException("OWNER_DESCRIPTOR_MISMATCH");
        if(handler.Protection?.Owner!=config.ExpectedOriginalOwner)
            throw new InvalidOperationException("OBSERVED_SOURCE_OWNER_NOT_AUTHORIZED");
        ct.ThrowIfCancellationRequested();
        return new(protectedState,handler.Label?.Label?.Id,handler.Protection?.Owner,
            descriptor?.UserRights?.Select(g=>new ObservedGrant(g.Users.ToArray(),g.Rights.ToArray())).ToArray(),
            descriptor is null ? null : descriptor.UserRoles is {Count:>0});
    }
}
