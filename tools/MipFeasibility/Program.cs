using Microsoft.InformationProtection;
using Microsoft.InformationProtection.File;
using Microsoft.InformationProtection.Protection;
using System.Reflection;
using System.Text.Json;

if (args is ["api"])
{
    foreach (var type in new[] { typeof(FileEngineSettings), typeof(IFileHandler), typeof(IProtectionHandler), typeof(ProtectionDescriptor), typeof(MipConfiguration), typeof(IFileEngine), typeof(UserRights), typeof(Label) })
    {
        Console.WriteLine(type.FullName);
        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Console.WriteLine("  " + member);
    }
    return 0;
}
if (args is ["run", var configPath]) return await LiveSpike.Run(configPath);
if (args is ["owner-only-preflight", var preflightPath]) return await OwnerOnlySpike.Run(preflightPath,false);
if (args is ["owner-only-run", var protectedPath]) return await OwnerOnlySpike.Run(protectedPath,true);
if (args is ["compare-protection", var authConfig, var manifest]) return await ProtectionComparison.Run(authConfig,manifest);
if (args is ["quarantine-owner-preflight", var receiptConfig]) return await QuarantineOwnerProbe.Run(receiptConfig,null);
if (args is ["quarantine-owner-inspect", var liveReceiptConfig, var receiptAuth]) return await QuarantineOwnerProbe.Run(liveReceiptConfig,receiptAuth);
if (args is ["defender-scanner-probe",var scannerConfig]) return await DefenderScannerProbe.Run(scannerConfig);
if (args is ["native-check"])
{
    try
    {
        MIP.Initialize(MipComponent.File);
        Console.WriteLine(JsonSerializer.Serialize(new { check = "native-load", result = "PASS", assembly = typeof(MIP).Assembly.GetName().Version?.ToString(), liveAuthentication = "NOT_RUN" }));
        return 0;
    }
    catch (Exception e)
    {
        Console.WriteLine(JsonSerializer.Serialize(new { check = "native-load", result = "FAIL", category = e.GetType().Name }));
        return 2;
    }
}
Console.WriteLine("MipFeasibility: api | native-check | run <private-config.json> | owner-only-preflight <private-config.json> | owner-only-run <private-config.json> | compare-protection <private-config.json> <hash-pinned-manifest.json> | quarantine-owner-preflight <private-receipt-config.json> | quarantine-owner-inspect <private-receipt-config.json> <private-owner-auth.json> | defender-scanner-probe <private-scanner-config.json>");
return args.Length == 0 ? 0 : 2;

