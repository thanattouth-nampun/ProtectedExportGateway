using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;
public sealed class DefenderNativeDiagnosticsTests
{
    private const string Target=@"C:\private\fixture.xlsx";
    private const string Output="Scan starting...\r\nScan finished.\r\nScanning "+Target+" found no threats.\r\n";
    private static readonly DateTimeOffset Now=DateTimeOffset.Parse("2026-10-26T08:00:00Z");
    private static DefenderHealthSnapshot Health=>new(true,true,true,"Normal",DefenderNativeDiagnostics.ReviewedBuild,"engine","definitions",Now.AddHours(-1),Now.AddSeconds(-10));
    [Fact] public void Exact_observed_text_is_not_a_trusted_clean_verdict()=>Assert.Equal("NATIVE_NO_THREATS_REPORTED_NOT_TRUSTED",DefenderNativeDiagnostics.ClassifyText(DefenderNativeDiagnostics.ReviewedBuild,Target,0,Output,""));
    [Theory]
    [InlineData("")]
    [InlineData("Scan starting...\nScan finished.\n")]
    [InlineData("Scan starting...\nScan finished.\nScanning C:\\other.xlsx found no threats.\n")]
    [InlineData("Scan starting...\nScan finished.\nScanning C:\\private\\fixture.xlsx found no threats.\nRemediation succeeded.\n")]
    [InlineData("Threat found and remediated successfully.")]
    [InlineData("Scan starting...\nScanning C:\\private\\fixture.xlsx found no threats.\nScan finished.\n")]
    public void Exit_zero_with_incomplete_other_target_extra_remediation_or_reordered_text_is_unknown(string output)=>Assert.Equal("NATIVE_TEXT_UNKNOWN",DefenderNativeDiagnostics.ClassifyText(DefenderNativeDiagnostics.ReviewedBuild,Target,0,output,""));
    [Theory][InlineData("new-build","")][InlineData(DefenderNativeDiagnostics.ReviewedBuild,"native warning")]
    public void Unreviewed_build_or_stderr_is_unknown(string build,string stderr)=>Assert.Equal("NATIVE_TEXT_UNKNOWN",DefenderNativeDiagnostics.ClassifyText(build,Target,0,Output,stderr));
    [Fact] public void Exit_two_is_not_automatically_detection()=>Assert.Equal("NATIVE_NONZERO_RESULT_UNCLASSIFIED",DefenderNativeDiagnostics.ClassifyText(DefenderNativeDiagnostics.ReviewedBuild,Target,2,"Scan failed.",""));
    [Theory][InlineData("service")][InlineData("antivirus")][InlineData("realtime")][InlineData("passive")][InlineData("stale")][InlineData("future")][InlineData("missing-engine")][InlineData("old-observation")]
    public void Disabled_stale_future_or_missing_health_blocks(string kind)
    {
        var health=kind switch {
            "service"=>Health with {ServiceEnabled=false},"antivirus"=>Health with {AntivirusEnabled=false},
            "realtime"=>Health with {RealTimeEnabled=false},"passive"=>Health with {Mode="Passive"},
            "stale"=>Health with {IntelligenceAt=Now.AddHours(-25)},"future"=>Health with {IntelligenceAt=Now.AddMinutes(1)},
            "missing-engine"=>Health with {EngineVersion=""},_=>Health with {ObservedAt=Now.AddMinutes(-6)}};
        Assert.False(health.IsFresh(Now));
    }
    [Fact] public void Matching_bracketed_health_remains_diagnostic()=>Assert.Equal("HEALTH_OBSERVED_NOT_GATE_PASS",DefenderNativeDiagnostics.HealthOutcome(Health,Health with {ObservedAt=Now},Now.AddSeconds(-5),Now.AddSeconds(-1),Now));
    [Fact] public void Definition_update_during_scan_rejects()=>Assert.Equal("SCANNER_HEALTH_CHANGED_DURING_RUN",DefenderNativeDiagnostics.HealthOutcome(Health,Health with {ObservedAt=Now,IntelligenceVersion="new"},Now.AddSeconds(-5),Now.AddSeconds(-1),Now));
    [Fact] public void Before_health_observed_after_start_is_invalid()=>Assert.Equal("SCANNER_HEALTH_STALE_DISABLED_OR_UNKNOWN",DefenderNativeDiagnostics.HealthOutcome(Health with {ObservedAt=Now},Health with {ObservedAt=Now},Now.AddSeconds(-5),Now.AddSeconds(-1),Now));
}
