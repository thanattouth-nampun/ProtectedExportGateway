using ClosedXML.Excel;

namespace Gateway.Application;

public static class SyntheticReports
{
    public const string DatasetVersion = "synthetic-hr-2026-09-v1";
    public static byte[] Generate(string reportId, string period)
    {
        if (period != "2026-09" || reportId is not ("headcount-summary" or "payroll-detail"))
            throw new ArgumentException("REQUEST_INVALID");
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Synthetic report");
        if (reportId == "headcount-summary")
        {
            sheet.Cell(1, 1).Value = "Department"; sheet.Cell(1, 2).Value = "Headcount";
            sheet.Cell(2, 1).Value = "Operations"; sheet.Cell(2, 2).Value = 6;
            sheet.Cell(3, 1).Value = "Finance"; sheet.Cell(3, 2).Value = 4;
        }
        else
        {
            var headers = new[] { "Synthetic employee ID", "Synthetic name", "Department", "Monthly salary", "Period" };
            for (var col = 0; col < headers.Length; col++) sheet.Cell(1, col + 1).Value = headers[col];
            for (var i = 1; i <= 10; i++)
            {
                sheet.Cell(i + 1, 1).Value = $"DEMO-{i:D3}"; sheet.Cell(i + 1, 2).Value = $"Synthetic person {i:D2}";
                sheet.Cell(i + 1, 3).Value = i <= 6 ? "Operations" : "Finance";
                sheet.Cell(i + 1, 4).Value = 30000 + i * 1000; sheet.Cell(i + 1, 5).Value = period;
            }
        }
        using var output = new MemoryStream();
        workbook.SaveAs(output, new SaveOptions { EvaluateFormulasBeforeSaving = false });
        if (output.Length > 10 * 1024 * 1024) throw new InvalidOperationException("ARTIFACT_TOO_LARGE");
        return output.ToArray(); // Private source bytes only; no HTTP/raw download adapter.
    }
}
