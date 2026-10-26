namespace Gateway.Application;

// Native invocation diagnostics are deliberately not ScanEvidence or a Clean verdict.
public static class ScannerProbeRules
{
    public static bool IsReviewedExecutablePath(string platformRoot,string executable)
    {
        if(!Path.IsPathFullyQualified(platformRoot) || !Path.IsPathFullyQualified(executable)) return false;
        var relative=Path.GetRelativePath(Path.GetFullPath(platformRoot),Path.GetFullPath(executable));
        return System.Text.RegularExpressions.Regex.IsMatch(relative,
            @"^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+-[0-9]+\\MpCmdRun\.exe$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);
    }
    public static string[] FixedArguments(string privateFile)=>["-Scan","-ScanType","3","-File",privateFile,"-DisableRemediation"];
    public static string Outcome(int? exitCode,bool timedOut,bool outputTruncated,string before,string after)
    {
        if(before.Length!=64 || before.Any(c=>!Uri.IsHexDigit(c)) || after!=before) return "SCANNER_INPUT_CHANGED_OR_UNKNOWN";
        if(timedOut) return "SCANNER_TIMEOUT_NATIVE_COMPLETION_UNKNOWN";
        if(outputTruncated) return "SCANNER_OUTPUT_TRUNCATED";
        return exitCode==0 ? "INVOCATION_COMPLETED_UNVERIFIED" : "SCANNER_ERROR_OR_DETECTION_UNVERIFIED";
    }
}
