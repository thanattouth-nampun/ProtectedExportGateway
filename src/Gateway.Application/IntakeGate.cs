using Gateway.Domain;

namespace Gateway.Application;

public interface ITrustedScanner
{
    Task<ScanEvidence> ScanAsync(string privateFileToken, CancellationToken cancellationToken);
}
public sealed class UnavailableScanner : ITrustedScanner
{
    public Task<ScanEvidence> ScanAsync(string privateFileToken, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("SCANNER_NOT_VALIDATED_G6");
}
public static class IntakeGate
{
    // Parser is never invoked before an independently trusted Clean verdict for exact bytes.
    public static void ValidateAfterScan(Stream file, string measuredHash, ScanEvidence scan, DateTimeOffset now)
    {
        if (!scan.IsTrustedClean(measuredHash, now)) throw new InvalidOperationException("SCAN_INDETERMINATE");
        var actual = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file));
        if (actual != measuredHash) throw new InvalidDataException("INPUT_HASH_CHANGED");
        file.Position = 0;
        BoundedXlsx.Validate(file);
        file.Position = 0;
        if (Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(file)) != measuredHash)
            throw new InvalidDataException("INPUT_HASH_CHANGED");
    }
}
