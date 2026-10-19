using System.Diagnostics;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace Gateway.Application;

public static class BoundedXlsx
{
    private static readonly XNamespace Spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private static readonly XNamespace Relationships = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string OfficeRelationships = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/";
    private static readonly HashSet<string> AllowedRelations = ["officeDocument", "worksheet", "styles", "sharedStrings", "theme", "extended-properties"];
    public static void Validate(Stream input)
    {
        if (!input.CanSeek || input.Length is <= 0 or > 10 * 1024 * 1024) Fail();
        var timer = Stopwatch.StartNew();
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        if (zip.Entries.Count is 0 or > 1000) Fail();
        long total = 0; var cells = 0; var sheets = 0;
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parts = new Dictionary<string, XDocument>(StringComparer.Ordinal);
        foreach (var entry in zip.Entries)
        {
            var name = entry.FullName;
            if (!names.Add(name) || name.Contains('\\') || name.StartsWith('/') || name.Contains('%')
                || name.Split('/').Any(s => s is ".." or "." or "") || name.Contains(':')) Fail();
            total = checked(total + entry.Length);
            if (total > 100 * 1024 * 1024 || entry.Length > Math.Max(1, entry.CompressedLength) * 100) Fail();
            // Deliberately small synthetic-workbook profile; unsupported parts are rejected, never executed.
            if (!(name is "[Content_Types].xml" or "_rels/.rels" or "xl/workbook.xml" or "xl/_rels/workbook.xml.rels"
                or "xl/styles.xml" or "xl/sharedStrings.xml" or "docProps/core.xml" or "docProps/app.xml"
                || System.Text.RegularExpressions.Regex.IsMatch(name, @"^xl/worksheets/sheet[1-9][0-9]*\.xml$", System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                || name == "xl/theme/theme1.xml"
                || System.Text.RegularExpressions.Regex.IsMatch(name, @"^package/services/metadata/core-properties/[a-fA-F0-9]{32}\.psmdcp$", System.Text.RegularExpressions.RegexOptions.CultureInvariant))) Fail();
            using var stream = entry.Open();
            using var bounded = new MemoryStream();
            var buffer = new byte[8192]; int read; long actual = 0;
            while ((read = stream.Read(buffer)) != 0)
            {
                actual += read;
                if (actual > entry.Length || actual > 100 * 1024 * 1024 || timer.Elapsed > TimeSpan.FromSeconds(60)) Fail();
                bounded.Write(buffer, 0, read);
            }
            if (actual != entry.Length) Fail();
            bounded.Position = 0;
            using var reader = XmlReader.Create(bounded, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null,
                MaxCharactersInDocument = 100 * 1024 * 1024, MaxCharactersFromEntities = 0
            });
            var doc = XDocument.Load(reader);
            parts.Add(name, doc);
            if (name.EndsWith(".rels", StringComparison.Ordinal))
            {
                if (doc.Root?.Name != Relationships + "Relationships") Fail();
                var ids = new HashSet<string>();
                foreach (var relation in doc.Root!.Elements())
                {
                    var type = (string?)relation.Attribute("Type") ?? "";
                    var target = (string?)relation.Attribute("Target") ?? "";
                    var core = type == "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties";
                    if (relation.Name != Relationships + "Relationship" || !ids.Add((string?)relation.Attribute("Id") ?? "")
                        || relation.Attribute("TargetMode") is not null || target.Length == 0
                        || target.Contains(':') || target.Contains('\\') || target.Contains('%') || target.Contains('?') || target.Contains('#')
                        || target.TrimStart('/').Split('/').Any(s => s is ".." or "." or "") || target.StartsWith("//", StringComparison.Ordinal)
                        || !(core || type.StartsWith(OfficeRelationships, StringComparison.Ordinal) && AllowedRelations.Contains(type[OfficeRelationships.Length..]))) Fail();
                    var resolved = target.StartsWith('/') ? target[1..] : name == "_rels/.rels" ? target : "xl/" + target;
                    if (!zip.Entries.Any(e => e.FullName == resolved)) Fail();
                }
            }
            if (name.StartsWith("xl/worksheets/", StringComparison.Ordinal))
            {
                if (doc.Root?.Name != Spreadsheet + "worksheet" || ++sheets > 50) Fail();
                cells += doc.Descendants(Spreadsheet + "c").Count();
                if (cells > 100_000) Fail();
            }
            // MVP accepts values only. Formula support can be expanded with a separately tested profile.
            if (doc.Descendants().Any(e => e.Name.LocalName is "f" or "externalReference" or "oleObject"
                or "sheetProtection" or "workbookProtection" or "extLst" or "AlternateContent")) Fail();
            if (timer.Elapsed > TimeSpan.FromSeconds(60)) Fail();
        }
        if (!parts.ContainsKey("[Content_Types].xml") || !parts.ContainsKey("_rels/.rels")
            || !parts.ContainsKey("xl/_rels/workbook.xml.rels") || !parts.TryGetValue("xl/workbook.xml", out var workbook)
            || workbook.Root?.Name != Spreadsheet + "workbook" || sheets == 0) Fail();
        var contentTypes = parts["[Content_Types].xml"];
        XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";
        var workbookType = contentTypes.Descendants(ct + "Override").SingleOrDefault(e => (string?)e.Attribute("PartName") == "/xl/workbook.xml")?.Attribute("ContentType")?.Value
            ?? contentTypes.Descendants(ct + "Default").SingleOrDefault(e => (string?)e.Attribute("Extension") == "xml")?.Attribute("ContentType")?.Value;
        if (contentTypes.Root?.Name != ct + "Types"
            || workbookType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"
            || contentTypes.Descendants().Attributes("ContentType").Any(a => a.Value.Contains("macro", StringComparison.OrdinalIgnoreCase))) Fail();
    }
    private static void Fail() => throw new InvalidDataException("CONTENT_VALIDATION_FAILED");
}
