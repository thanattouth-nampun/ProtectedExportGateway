using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gateway.Application;
using Gateway.Infrastructure.Persistence;

internal sealed record ScannerProbeConfig(string PrivateRoot,string ExecutablePath,string ExecutableSha256,
    string AuthorizationReference,bool SyntheticScanAcknowledged)
{
    public string Scenario {get;init;}="BenignWorkbook";
    public bool ControlledDetectionAcknowledged {get;init;}
}

// Ordinary-user, one benign generated workbook only. Not a privileged helper or production scanner adapter.
internal static class DefenderScannerProbe
{
    public static async Task<int> Run(string configPath)
    {
        var stage="config";
        var nativeScanStarted=false;Guid? attemptedRun=null,attemptedFixture=null;
        try
        {
            var c=JsonSerializer.Deserialize<ScannerProbeConfig>(await File.ReadAllTextAsync(configPath),OwnerOnlySpike.Json)
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            if(!OperatingSystem.IsWindows() || !c.SyntheticScanAcknowledged || string.IsNullOrWhiteSpace(c.AuthorizationReference)
                || !Path.IsPathFullyQualified(c.PrivateRoot) || c.ExecutableSha256.Length!=64 || c.ExecutableSha256.Any(x=>!Uri.IsHexDigit(x))
                || c.Scenario is not ("BenignWorkbook" or "MissingTarget" or "VendorEicarZip"))
                throw new InvalidOperationException("CONFIG_INVALID");
            if(c.Scenario=="VendorEicarZip" && !c.ControlledDetectionAcknowledged)
                throw new InvalidOperationException("CONTROLLED_DETECTION_NOT_APPROVED");
            if(c.Scenario=="VendorEicarZip" && Path.GetFullPath(c.PrivateRoot)!=Path.GetFullPath(".runtime/scanner-detection-control"))
                throw new InvalidOperationException("CONTROLLED_DETECTION_ROOT_REJECTED");
            using var identity=WindowsIdentity.GetCurrent();
            if(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("NON_ADMIN_PROBE_REQUIRED");
            _=new LocalQuarantineObjects(c.PrivateRoot); // Reviewed private DACL + reparse ancestors.
            if(c.Scenario=="VendorEicarZip")
            {
                // One-shot, create-only claim: failures never silently authorize a second vendor request.
                // Do not remove/reset this marker automatically; the approved scope must be reviewed again.
                var claim=Path.Combine(c.PrivateRoot,"controlled-attempt.claim");
                if(File.Exists(claim)) throw new InvalidOperationException("CONTROLLED_DETECTION_ATTEMPT_ALREADY_USED");
                using var permit=new FileStream(claim,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                var record=Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {reservedAt=DateTimeOffset.UtcNow,c.AuthorizationReference}));
                permit.Write(record);permit.Flush(true);
            }
            var platform=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Microsoft","Windows Defender","Platform");
            var executable=Path.GetFullPath(c.ExecutablePath);
            if(!ScannerProbeRules.IsReviewedExecutablePath(platform,executable))
                throw new InvalidOperationException("DEFENDER_EXECUTABLE_PATH_REJECTED");
            for(FileSystemInfo? item=new FileInfo(executable);item is not null;item=item is FileInfo f ? f.Directory : ((DirectoryInfo)item).Parent)
                if((item.Attributes&FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("DEFENDER_EXECUTABLE_REPARSE_REJECTED");
            using var binary=new FileStream(executable,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(Convert.ToHexString(SHA256.HashData(binary))!=c.ExecutableSha256) throw new InvalidOperationException("DEFENDER_EXECUTABLE_HASH_CHANGED");
            var runId=Guid.NewGuid();var token=Guid.NewGuid();
            attemptedRun=runId;attemptedFixture=token;
            var filePath=Path.Combine(c.PrivateRoot,token.ToString("N")+(c.Scenario=="VendorEicarZip"?".zip":".xlsx"));
            // Prove health before downloading/writing controlled detection material, too.
            stage="native-health-before";
            var healthBefore=await ReadHealth();
            if(!healthBefore.IsFresh(DateTimeOffset.UtcNow)) throw new InvalidOperationException("SCANNER_HEALTH_STALE_DISABLED_OR_UNKNOWN");
            if(c.Scenario!="MissingTarget")
            {
                stage=c.Scenario=="VendorEicarZip"?"approved-vendor-fixture-fetch":"generated-benign-fixture";
                var bytes=c.Scenario=="VendorEicarZip" ? await FetchVendorControl() : SyntheticReports.Generate("headcount-summary","2026-09");
                stage=c.Scenario=="VendorEicarZip"?"approved-vendor-fixture-write":"generated-benign-fixture-write";
                using var created=new FileStream(filePath,FileMode.CreateNew,FileAccess.Write,FileShare.None);
                created.Write(bytes);created.Flush(true);
            }
            else if(File.Exists(filePath)) throw new InvalidOperationException("MISSING_TARGET_CONTROL_COLLISION");
            using var locked=c.Scenario!="MissingTarget" ? new FileStream(filePath,FileMode.Open,FileAccess.Read,FileShare.Read) : null;
            var before=locked is null ? null : Convert.ToHexString(SHA256.HashData(locked));
            stage="ordinary-user-fixed-native-scan";
            var start=new ProcessStartInfo(executable) {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var arg in ScannerProbeRules.FixedArguments(filePath)) start.ArgumentList.Add(arg);
            var startedAt=DateTimeOffset.UtcNow;
            using var process=Process.Start(start) ?? throw new InvalidOperationException("SCANNER_PROCESS_NOT_STARTED");
            nativeScanStarted=true;
            var stdout=Capture(process.StandardOutput);var stderr=Capture(process.StandardError);
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var timedOut=false;
            try {await process.WaitForExitAsync(deadline.Token);}
            catch(OperationCanceledException)
            {
                timedOut=true;
                // Stopping the CLI does NOT prove that the AV service cancelled its internal operation.
                try {process.Kill(entireProcessTree:true);} catch(InvalidOperationException) { }
                await process.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            await Task.WhenAll(stdout,stderr).WaitAsync(TimeSpan.FromSeconds(5));
            var output=await stdout;var error=await stderr;
            var completedAt=DateTimeOffset.UtcNow;
            string? after=null;
            if(locked is not null) {locked.Position=0;after=Convert.ToHexString(SHA256.HashData(locked));}
            binary.Position=0;
            if(Convert.ToHexString(SHA256.HashData(binary))!=c.ExecutableSha256) throw new InvalidOperationException("DEFENDER_EXECUTABLE_HASH_CHANGED");
            stage="native-health-after";
            var healthAfter=await ReadHealth();
            var healthOutcome=DefenderNativeDiagnostics.HealthOutcome(healthBefore,healthAfter,startedAt,completedAt,DateTimeOffset.UtcNow);
            var build=FileVersionInfo.GetVersionInfo(executable).FileVersion?.Split(' ')[0]??"";
            var textObservation=DefenderNativeDiagnostics.ClassifyText(build,filePath,process.ExitCode,output.Text,error.Text);
            var outcome=before is null ? "MISSING_TARGET_CONTROL_NOT_RELEASE_EVIDENCE"
                : ScannerProbeRules.Outcome(process.ExitCode,timedOut,output.Truncated||error.Truncated,before,after??"");
            var report=new {check="ordinary-user-defender-probe",runId,fixtureToken=token,
                fixtureSource=c.Scenario=="VendorEicarZip"?"https://secure.eicar.org/eicar.com.zip":SyntheticReports.DatasetVersion,
                scenario=c.Scenario,bytes=locked?.Length,beforeHash=before,afterHash=after,executableSha256=c.ExecutableSha256,
                executableVersion=FileVersionInfo.GetVersionInfo(executable).FileVersion,
                nativeExitCode=process.ExitCode,timedOut,outputTruncated=output.Truncated||error.Truncated,outcome,
                nativeTextProfile=DefenderNativeDiagnostics.Profile,textObservation,healthOutcome,healthBefore,healthAfter,
                startedAt,completedAt,targetExistsAfter=File.Exists(filePath),
                stdoutHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output.Text))),
                workerElevated=false,cleanVerdict="NOT_ESTABLISHED",canPublish=false,gatePassed=false};
            // Keep bounded raw command text private for semantic review; never return it through an API.
            await File.WriteAllTextAsync(Path.Combine(c.PrivateRoot,runId.ToString("N")+".native.txt"),output.Text+"\nSTDERR:\n"+error.Text);
            await File.WriteAllTextAsync(Path.Combine(c.PrivateRoot,runId.ToString("N")+".json"),JsonSerializer.Serialize(report,OwnerOnlySpike.Json));
            Console.WriteLine(JsonSerializer.Serialize(report,OwnerOnlySpike.Json));
            return outcome=="INVOCATION_COMPLETED_UNVERIFIED" && textObservation=="NATIVE_NO_THREATS_REPORTED_NOT_TRUSTED"
                && healthOutcome=="HEALTH_OBSERVED_NOT_GATE_PASS" ? 3 : 2;
        }
        catch(Exception e)
        {
            var code=e is InvalidOperationException && Regex.IsMatch(e.Message,"^[A-Z_]{3,80}$") ? e.Message : null;
            Console.WriteLine(JsonSerializer.Serialize(new {check="ordinary-user-defender-probe",status="FAILED",stage,code,
                runId=attemptedRun,fixtureToken=attemptedFixture,nativeScanStarted,
                httpStatus=e is HttpRequestException request ? request.StatusCode : null,
                category=e.GetType().Name,hresult=e.HResult.ToString("X8"),gatePassed=false}));
            return 2;
        }
    }
    private static async Task<byte[]> FetchVendorControl()
    {
        // No mirror, redirects, arbitrary URL, executable content launch or protection bypass.
        using var handler=new HttpClientHandler {AllowAutoRedirect=false};
        using var http=new HttpClient(handler) {Timeout=TimeSpan.FromSeconds(15)};
        using var response=await http.GetAsync("https://secure.eicar.org/eicar.com.zip",HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await using var stream=await response.Content.ReadAsStreamAsync(deadline.Token);
        var data=new byte[185];var length=0;
        while(length<data.Length)
        {
            var read=await stream.ReadAsync(data.AsMemory(length),deadline.Token);if(read==0) break;length+=read;
        }
        if(length!=184) throw new InvalidOperationException("VENDOR_FIXTURE_SIZE_REJECTED");
        var archive=data[..length];
        // Bounded known vendor fixture validation, not the production upload parser/scan ordering.
        using var zip=new ZipArchive(new MemoryStream(archive),ZipArchiveMode.Read);
        if(zip.Entries.Count!=1 || zip.Entries[0].FullName!="eicar.com" || zip.Entries[0].Length!=68)
            throw new InvalidOperationException("VENDOR_FIXTURE_PROFILE_REJECTED");
        using var entry=zip.Entries[0].Open();var body=new byte[69];var count=0;
        while(count<body.Length) {var read=await entry.ReadAsync(body.AsMemory(count),deadline.Token);if(read==0) break;count+=read;}
        // Standard 68-byte EICAR payload; no payload text is included in source/logs or executed.
        if(count!=68 || Convert.ToHexString(SHA256.HashData(body.AsSpan(0,count)))!="275A021BBFB6489E54D471899F7DB9D1663FC695EC2FE2A2C4538AABF651FD0F")
            throw new InvalidOperationException("VENDOR_FIXTURE_HASH_REJECTED");
        return archive;
    }
    private static async Task<DefenderHealthSnapshot> ReadHealth()
    {
        // Native Windows PowerShell avoids PS7 compatibility-remoting; this is fixed read-only code.
        var executable=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"WindowsPowerShell","v1.0","powershell.exe");
        const string script="$ErrorActionPreference='Stop'; Import-Module Defender; $s=Get-MpComputerStatus; [ordered]@{ServiceEnabled=[bool]$s.AMServiceEnabled; AntivirusEnabled=[bool]$s.AntivirusEnabled; RealTimeEnabled=[bool]$s.RealTimeProtectionEnabled; Mode=$s.AMRunningMode; ProductVersion=$s.AMProductVersion; EngineVersion=$s.AMEngineVersion; IntelligenceVersion=$s.AntivirusSignatureVersion; IntelligenceAt=$s.AntivirusSignatureLastUpdated.ToUniversalTime().ToString('O'); ObservedAt=[DateTimeOffset]::UtcNow.ToString('O')} | ConvertTo-Json -Compress";
        var start=new ProcessStartInfo(executable) {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"-NoProfile","-NonInteractive","-Command",script}) start.ArgumentList.Add(arg);
        using var process=Process.Start(start)??throw new InvalidOperationException("SCANNER_HEALTH_UNAVAILABLE");
        var stdout=Capture(process.StandardOutput);var stderr=Capture(process.StandardError);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try {await process.WaitForExitAsync(deadline.Token);}
        catch(OperationCanceledException)
        {
            try {process.Kill(entireProcessTree:true);} catch(InvalidOperationException) { }
            throw new InvalidOperationException("SCANNER_HEALTH_TIMEOUT");
        }
        await Task.WhenAll(stdout,stderr).WaitAsync(TimeSpan.FromSeconds(5));
        var output=await stdout;var error=await stderr;
        if(process.ExitCode!=0 || output.Truncated || error.Truncated || !string.IsNullOrWhiteSpace(error.Text))
            throw new InvalidOperationException("SCANNER_HEALTH_UNAVAILABLE");
        return JsonSerializer.Deserialize<DefenderHealthSnapshot>(output.Text,OwnerOnlySpike.Json)
            ??throw new InvalidOperationException("SCANNER_HEALTH_UNAVAILABLE");
    }
    private static async Task<(string Text,bool Truncated)> Capture(StreamReader reader)
    {
        const int maximum=16384;var result=new StringBuilder();var buffer=new char[1024];var truncated=false;int count;
        while((count=await reader.ReadAsync(buffer))!=0)
        {
            var keep=Math.Min(count,maximum-result.Length);result.Append(buffer,0,keep);truncated|=keep<count;
        }
        return(result.ToString(),truncated);
    }
}
