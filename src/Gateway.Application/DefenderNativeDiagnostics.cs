namespace Gateway.Application;

// Native observations, not a production scanner attestation or ScanEvidence.
public sealed record DefenderHealthSnapshot(bool ServiceEnabled,bool AntivirusEnabled,bool RealTimeEnabled,
    string Mode,string ProductVersion,string EngineVersion,string IntelligenceVersion,
    DateTimeOffset IntelligenceAt,DateTimeOffset ObservedAt)
{
    public bool IsFresh(DateTimeOffset now)=>ServiceEnabled && AntivirusEnabled && RealTimeEnabled && Mode=="Normal"
        && !string.IsNullOrWhiteSpace(ProductVersion) && !string.IsNullOrWhiteSpace(EngineVersion)
        && !string.IsNullOrWhiteSpace(IntelligenceVersion) && IntelligenceAt<=ObservedAt && ObservedAt<=now
        && now-IntelligenceAt<=TimeSpan.FromHours(24) && now-ObservedAt<=TimeSpan.FromMinutes(5);
}

public static class DefenderNativeDiagnostics
{
    public const string ReviewedBuild="4.18.26090.9";
    public const string Profile="MpCmdRun-4.18.26090.9-en-US-diagnostic-v1";
    public static string ClassifyText(string build,string target,int exitCode,string stdout,string stderr)
    {
        if(build!=ReviewedBuild || string.IsNullOrWhiteSpace(target) || target.Any(char.IsControl)
            || !string.IsNullOrWhiteSpace(stderr)) return "NATIVE_TEXT_UNKNOWN";
        var lines=stdout.Split('\n').Select(x=>x.TrimEnd('\r')).Where(x=>x.Length>0).ToArray();
        // Exact observed output for this build/language only; no substring/absence-of-alert inference.
        if(exitCode==0 && lines.SequenceEqual(new[]{"Scan starting...","Scan finished.",$"Scanning {target} found no threats."}))
            return "NATIVE_NO_THREATS_REPORTED_NOT_TRUSTED";
        if(exitCode!=0) return "NATIVE_NONZERO_RESULT_UNCLASSIFIED"; // 2 can mean detection OR scan failure.
        return "NATIVE_TEXT_UNKNOWN";
    }
    public static string HealthOutcome(DefenderHealthSnapshot before,DefenderHealthSnapshot after,
        DateTimeOffset startedAt,DateTimeOffset completedAt,DateTimeOffset now)
    {
        if(startedAt>completedAt || completedAt>now || now-completedAt>TimeSpan.FromMinutes(5)
            || before.ObservedAt>startedAt || startedAt-before.ObservedAt>TimeSpan.FromMinutes(5)
            || after.ObservedAt<completedAt || !before.IsFresh(now) || !after.IsFresh(now))
            return "SCANNER_HEALTH_STALE_DISABLED_OR_UNKNOWN";
        return before.ProductVersion==after.ProductVersion && before.EngineVersion==after.EngineVersion
            && before.IntelligenceVersion==after.IntelligenceVersion && before.IntelligenceAt==after.IntelligenceAt
            ? "HEALTH_OBSERVED_NOT_GATE_PASS" : "SCANNER_HEALTH_CHANGED_DURING_RUN";
    }
}
