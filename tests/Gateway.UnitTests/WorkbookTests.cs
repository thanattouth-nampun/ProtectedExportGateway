using System.IO.Compression;
using System.Text;
using Gateway.Application;
using Gateway.Domain;
using Xunit;

namespace Gateway.UnitTests;
public class WorkbookTests
{
    [Theory][InlineData("headcount-summary")][InlineData("payroll-detail")]
    public void Synthetic_generator_has_bounded_valid_value_only_package(string report)
    {
        using var bytes = new MemoryStream(SyntheticReports.Generate(report, "2026-09"));
        BoundedXlsx.Validate(bytes);
        if (report == "headcount-summary")
        {
            bytes.Position = 0; using var zip = new ZipArchive(bytes);
            var all = string.Join("", zip.Entries.Select(e => { using var r = new StreamReader(e.Open()); return r.ReadToEnd(); }));
            Assert.DoesNotContain("salary", all, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Synthetic person", all);
        }
    }
    [Theory][InlineData("../escape.xml")][InlineData("xl/vbaProject.bin")][InlineData("xl/embeddings/oleObject1.bin")][InlineData("_xmlsignatures/sig1.xml")]
    public void Unsafe_or_unsupported_part_is_rejected(string name)
    {
        using var file = Mutated(zip => { var entry = zip.CreateEntry(name); using var writer = new StreamWriter(entry.Open()); writer.Write("<unsafe/>"); });
        Assert.Throws<InvalidDataException>(() => BoundedXlsx.Validate(file));
    }
    [Fact] public void Duplicate_case_ambiguous_entry_is_rejected()
    {
        using var file = Mutated(zip => { using var w = new StreamWriter(zip.CreateEntry("XL/workbook.xml").Open()); w.Write("<x/>"); });
        Assert.Throws<InvalidDataException>(() => BoundedXlsx.Validate(file));
    }
    [Fact] public void Dtd_is_prohibited()
    {
        using var file = Mutated(zip => { zip.GetEntry("xl/workbook.xml")!.Delete(); using var w = new StreamWriter(zip.CreateEntry("xl/workbook.xml").Open());
            w.Write("<!DOCTYPE workbook [<!ENTITY x SYSTEM 'file:///never-read'>]><workbook>&x;</workbook>"); });
        Assert.ThrowsAny<Exception>(() => BoundedXlsx.Validate(file));
    }
    [Fact] public void External_relationship_is_rejected()
    {
        using var file = Mutated(zip => { zip.GetEntry("xl/_rels/workbook.xml.rels")!.Delete(); using var w = new StreamWriter(zip.CreateEntry("xl/_rels/workbook.xml.rels").Open());
            w.Write("<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='rId1' TargetMode='External' Target='https://example.invalid/x' Type='http://schemas.openxmlformats.org/officeDocument/2006/relationships/externalLink'/></Relationships>"); });
        Assert.Throws<InvalidDataException>(() => BoundedXlsx.Validate(file));
    }
    [Fact] public void High_ratio_entry_is_rejected()
    {
        using var file = Mutated(zip => { using var w = new StreamWriter(zip.CreateEntry("xl/sharedStrings.xml", CompressionLevel.SmallestSize).Open()); w.Write(new string('A', 1024 * 1024)); });
        Assert.Throws<InvalidDataException>(() => BoundedXlsx.Validate(file));
    }
    [Fact] public void Scanner_gate_runs_before_parser_even_for_invalid_zip()
    {
        using var file = new MemoryStream(Encoding.UTF8.GetBytes("not a ZIP"));
        var now = DateTimeOffset.UtcNow;
        var scan = new ScanEvidence(ExecutionMode.Simulated, ScanVerdict.Clean, "", "", "fake", "", "", now, now, "run");
        var error = Assert.Throws<InvalidOperationException>(() => IntakeGate.ValidateAfterScan(file, "", scan, now));
        Assert.Equal("SCAN_INDETERMINATE", error.Message);
    }
    [Fact] public void Claimed_hash_does_not_replace_measured_bytes()
    {
        using var file = new MemoryStream(SyntheticReports.Generate("headcount-summary", "2026-09"));
        var now = DateTimeOffset.UtcNow; var hash = new string('A', 64);
        var scan = new ScanEvidence(ExecutionMode.LiveMip, ScanVerdict.Clean, hash, hash, "test-only", "engine", "intel", now, now, "run");
        Assert.Throws<InvalidDataException>(() => IntakeGate.ValidateAfterScan(file, hash, scan, now));
    }
    private static MemoryStream Mutated(Action<ZipArchive> mutation)
    {
        var memory = new MemoryStream(); memory.Write(SyntheticReports.Generate("headcount-summary", "2026-09")); memory.Position = 0;
        using (var zip = new ZipArchive(memory, ZipArchiveMode.Update, leaveOpen: true)) mutation(zip);
        memory.Position = 0; return memory;
    }
}
