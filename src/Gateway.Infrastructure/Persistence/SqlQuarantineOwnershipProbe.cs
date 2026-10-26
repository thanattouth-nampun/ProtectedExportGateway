using System.Security.Cryptography;
using Gateway.Application;
using Gateway.Domain;
using Microsoft.EntityFrameworkCore;

namespace Gateway.Infrastructure.Persistence;

// Read-only diagnostic: no DB writes, source promotion or security-state transition.
// A production worker still needs durable leases/evidence publication and Azure immutable versions.
public sealed class SqlQuarantineOwnershipProbe(DbContextOptions<GatewayDbContext> options,
    PortalIdentityDirectory directory,LocalQuarantineObjects objects)
{
    public async Task<QuarantineOwnershipReport> Run(Guid tenant,Guid version,string expectedHash,string approvedLabel,
        IProtectedOwnershipInspector? inspector,CancellationToken ct=default)
    {
        if(tenant!=directory.TenantId || version==Guid.Empty || !Guid.TryParse(approvedLabel,out var labelId)
            || labelId==Guid.Empty || expectedHash.Length!=64 || expectedHash.Any(c=>!Uri.IsHexDigit(c)))
            throw new InvalidOperationException("OWNERSHIP_TARGET_INVALID");
        await using var db=new GatewayDbContext(options);
        var row=await db.DocumentVersions.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenant && x.Id==version,ct)
            ?? throw new InvalidOperationException("QUARANTINE_VERSION_NOT_FOUND");
        var doc=await db.Documents.AsNoTracking().SingleOrDefaultAsync(x=>x.TenantId==tenant && x.Id==row.DocumentId,ct);
        if(doc is null || !doc.Enabled || doc.Status!=PublicationStatus.Draft || doc.CurrentPublishedVersionId is not null
            || doc.OwnerOid!=row.UploaderOid || row.State!=IntakeState.AwaitingValidation || row.UploadKey is null
            || row.QuarantineToken is null || row.UploadLeaseUntil is not null
            || row.SourceBindingJson is not null || row.ScanEvidenceJson is not null || row.ValidationEvidenceJson is not null
            || row.QuarantineSha256!=expectedHash || row.QuarantineBytes is null or <=0 or >QuarantineLimits.MaxBytes)
            throw new InvalidOperationException("QUARANTINE_VERSION_NOT_INSPECTABLE");
        var mapping=directory.Resolve(new(tenant,row.UploaderOid))
            ?? throw new InvalidOperationException("UPLOADER_RMS_MAPPING_NOT_REVIEWED");
        await using var file=objects.OpenInspection(row.QuarantineToken.Value);
        async Task CheckBytes()
        {
            file.Position=0;
            if(file.Length!=row.QuarantineBytes || Convert.ToHexString(await SHA256.HashDataAsync(file,ct))!=expectedHash)
                throw new InvalidOperationException("QUARANTINE_BYTES_CHANGED");
            file.Position=0;
        }
        await CheckBytes();
        var outcome="LOCAL_PREFLIGHT_ONLY_INSPECTION_NOT_RUN";
        if(inspector is not null)
        {
            // The SDK receives a read-only locked stream, not an arbitrary upload path or address.
            var observed=await inspector.Inspect(file,"quarantine-"+row.Id.ToString("N")+".xlsx",mapping.RmsAddress,ct);
            ct.ThrowIfCancellationRequested();
            outcome=QuarantineOwnershipRules.Evaluate(observed,approvedLabel,mapping.RmsAddress);
        }
        await CheckBytes();
        var latest=await db.DocumentVersions.AsNoTracking().SingleAsync(x=>x.Id==row.Id,ct);
        var latestDoc=await db.Documents.AsNoTracking().SingleAsync(x=>x.Id==doc.Id,ct);
        if(!latest.RowVersion.SequenceEqual(row.RowVersion) || !latestDoc.RowVersion.SequenceEqual(doc.RowVersion))
            throw new InvalidOperationException("QUARANTINE_SNAPSHOT_CHANGED");
        return new(tenant,doc.Id,row.Id,row.UploaderOid,expectedHash,row.QuarantineBytes.Value,mapping.ReviewReference,outcome);
    }
}
