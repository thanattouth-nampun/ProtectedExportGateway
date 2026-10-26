using Gateway.Application;
using Xunit;

namespace Gateway.UnitTests;
public sealed class ScannerProbeRulesTests
{
    private static readonly string Hash=new('A',64);
    private const string Root=@"C:\ProgramData\Microsoft\Windows Defender\Platform";
    [Fact] public void Only_numeric_version_directory_of_reviewed_Defender_root_is_allowed()=>Assert.True(
        ScannerProbeRules.IsReviewedExecutablePath(Root,Root+@"\4.18.26090.9-0\MpCmdRun.exe"));
    [Theory]
    [InlineData(@"C:\Temp\4.18.26090.9-0\MpCmdRun.exe")]
    [InlineData(@"C:\ProgramData\Microsoft\Windows Defender\Platform\CurrentVersion\MpCmdRun.exe")]
    [InlineData(@"C:\ProgramData\Microsoft\Windows Defender\Platform\4.18.26090.9-0\cmd.exe")]
    [InlineData(@"C:\ProgramData\Microsoft\Windows Defender\Platform\..\4.18.26090.9-0\MpCmdRun.exe")]
    [InlineData(@"4.18.26090.9-0\MpCmdRun.exe")]
    public void Unreviewed_executables_and_relative_paths_reject(string executable)=>Assert.False(ScannerProbeRules.IsReviewedExecutablePath(Root,executable));
    [Fact] public void One_path_is_one_argument_even_with_spaces_or_option_like_text()
    {
        var path=@"C:\private root\-SignatureUpdate.xlsx";
        Assert.Equal(new[]{"-Scan","-ScanType","3","-File",path,"-DisableRemediation"},ScannerProbeRules.FixedArguments(path));
    }
    [Fact] public void Native_exit_zero_never_establishes_clean()=>Assert.Equal("INVOCATION_COMPLETED_UNVERIFIED",ScannerProbeRules.Outcome(0,false,false,Hash,Hash));
    [Theory][InlineData(2)][InlineData(-1)][InlineData(null)]
    public void Errors_or_detection_remain_unknown(int? code)=>Assert.Equal("SCANNER_ERROR_OR_DETECTION_UNVERIFIED",ScannerProbeRules.Outcome(code,false,false,Hash,Hash));
    [Fact] public void Timeout_does_not_claim_native_service_cancelled()=>Assert.Equal("SCANNER_TIMEOUT_NATIVE_COMPLETION_UNKNOWN",ScannerProbeRules.Outcome(0,true,false,Hash,Hash));
    [Fact] public void Truncated_output_is_not_success()=>Assert.Equal("SCANNER_OUTPUT_TRUNCATED",ScannerProbeRules.Outcome(0,false,true,Hash,Hash));
    [Theory][InlineData("wrong")][InlineData("")]
    public void Changed_or_unknown_bytes_fail(string after)=>Assert.Equal("SCANNER_INPUT_CHANGED_OR_UNKNOWN",ScannerProbeRules.Outcome(0,false,false,Hash,after));
}
