using Gateway.Application;
using System.IO.Compression;

// Creates private synthetic input only. Never inserts an Eligible or Published record.
if (args is not ["generate", var report, var destination] || !Path.IsPathFullyQualified(destination))
{
    Console.WriteLine("SeedDocuments generate <headcount-summary|payroll-detail> <absolute-private-xlsx-path>"); return 2;
}
var bytes = SyntheticReports.Generate(report, "2026-09");
using var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None);
stream.Write(bytes);
using var memory = new MemoryStream(bytes); using var zip = new ZipArchive(memory);
foreach (var entry in zip.Entries) Console.WriteLine(entry.FullName);
Console.WriteLine("Synthetic private source created. Scan/intake/publish NOT RUN.");
return 0;
