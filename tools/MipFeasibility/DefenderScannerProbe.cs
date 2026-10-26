using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Gateway.Application;
using Gateway.Infrastructure.Persistence;

internal sealed record ScannerProbeConfig(string PrivateRoot,string ExecutablePath,string ExecutableSha256,
    string AuthorizationReference,bool SyntheticScanAcknowledged);

// Ordinary-user, one benign generated workbook only. Not a privileged helper or production scanner adapter.
internal static class DefenderScannerProbe
{
    public static async Task<int> Run(string configPath)
    {
        var stage="config";
        try
        {
            var c=JsonSerializer.Deserialize<ScannerProbeConfig>(await File.ReadAllTextAsync(configPath),OwnerOnlySpike.Json)
                ?? throw new InvalidOperationException("CONFIG_INVALID");
            if(!OperatingSystem.IsWindows() || !c.SyntheticScanAcknowledged || string.IsNullOrWhiteSpace(c.AuthorizationReference)
                || !Path.IsPathFullyQualified(c.PrivateRoot) || c.ExecutableSha256.Length!=64 || c.ExecutableSha256.Any(x=>!Uri.IsHexDigit(x)))
                throw new InvalidOperationException("CONFIG_INVALID");
            using var identity=WindowsIdentity.GetCurrent();
            if(new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("NON_ADMIN_PROBE_REQUIRED");
            _=new LocalQuarantineObjects(c.PrivateRoot); // Reviewed private DACL + reparse ancestors.
            var platform=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"Microsoft","Windows Defender","Platform");
            var executable=Path.GetFullPath(c.ExecutablePath);
            if(!ScannerProbeRules.IsReviewedExecutablePath(platform,executable))
                throw new InvalidOperationException("DEFENDER_EXECUTABLE_PATH_REJECTED");
            for(FileSystemInfo? item=new FileInfo(executable);item is not null;item=item is FileInfo f ? f.Directory : ((DirectoryInfo)item).Parent)
                if((item.Attributes&FileAttributes.ReparsePoint)!=0) throw new InvalidOperationException("DEFENDER_EXECUTABLE_REPARSE_REJECTED");
            using var binary=new FileStream(executable,FileMode.Open,FileAccess.Read,FileShare.Read);
            if(Convert.ToHexString(SHA256.HashData(binary))!=c.ExecutableSha256) throw new InvalidOperationException("DEFENDER_EXECUTABLE_HASH_CHANGED");
            var runId=Guid.NewGuid();var token=Guid.NewGuid();
            var filePath=Path.Combine(c.PrivateRoot,token.ToString("N")+".xlsx");
            var bytes=SyntheticReports.Generate("headcount-summary","2026-09");
            using(var created=new FileStream(filePath,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {created.Write(bytes);created.Flush(true);}
            using var locked=new FileStream(filePath,FileMode.Open,FileAccess.Read,FileShare.Read);
            var before=Convert.ToHexString(SHA256.HashData(locked));
            stage="ordinary-user-fixed-native-scan";
            var start=new ProcessStartInfo(executable) {UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            foreach(var arg in ScannerProbeRules.FixedArguments(filePath)) start.ArgumentList.Add(arg);
            using var process=Process.Start(start) ?? throw new InvalidOperationException("SCANNER_PROCESS_NOT_STARTED");
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
            locked.Position=0;var after=Convert.ToHexString(SHA256.HashData(locked));
            binary.Position=0;
            if(Convert.ToHexString(SHA256.HashData(binary))!=c.ExecutableSha256) throw new InvalidOperationException("DEFENDER_EXECUTABLE_HASH_CHANGED");
            var outcome=ScannerProbeRules.Outcome(process.ExitCode,timedOut,output.Truncated||error.Truncated,before,after);
            var report=new {check="ordinary-user-defender-probe",runId,fixtureToken=token,syntheticGenerator=SyntheticReports.DatasetVersion,
                bytes=locked.Length,beforeHash=before,afterHash=after,executableSha256=c.ExecutableSha256,
                executableVersion=FileVersionInfo.GetVersionInfo(executable).FileVersion,
                nativeExitCode=process.ExitCode,timedOut,outputTruncated=output.Truncated||error.Truncated,outcome,
                stdoutHash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(output.Text))),
                workerElevated=false,cleanVerdict="NOT_ESTABLISHED",canPublish=false,gatePassed=false};
            // Keep bounded raw command text private for semantic review; never return it through an API.
            await File.WriteAllTextAsync(Path.Combine(c.PrivateRoot,runId.ToString("N")+".native.txt"),output.Text+"\nSTDERR:\n"+error.Text);
            await File.WriteAllTextAsync(Path.Combine(c.PrivateRoot,runId.ToString("N")+".json"),JsonSerializer.Serialize(report,OwnerOnlySpike.Json));
            Console.WriteLine(JsonSerializer.Serialize(report,OwnerOnlySpike.Json));
            return outcome=="INVOCATION_COMPLETED_UNVERIFIED" ? 3 : 2;
        }
        catch(Exception e)
        {
            var code=e is InvalidOperationException && Regex.IsMatch(e.Message,"^[A-Z_]{3,80}$") ? e.Message : null;
            Console.WriteLine(JsonSerializer.Serialize(new {check="ordinary-user-defender-probe",status="FAILED",stage,code,category=e.GetType().Name,gatePassed=false}));
            return 2;
        }
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
