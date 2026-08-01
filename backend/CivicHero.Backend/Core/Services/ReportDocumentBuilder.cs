using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using CivicHero.Backend.Core.DTOs.Analytics;

namespace CivicHero.Backend.Core.Services;

public sealed record ReportTable(
    string Title,
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    string? Subtitle = null);

public static class ReportDocumentBuilder
{
    private const int MaxExportRows = 5000;

    public static AnalyticsExportResult Build(ReportTable table, string? format, string baseFileName)
    {
        ArgumentNullException.ThrowIfNull(table);
        if (table.Headers.Count == 0) throw new ArgumentException("A report must contain at least one column.", nameof(table));
        if (table.Rows.Any(row => row.Count != table.Headers.Count))
            throw new ArgumentException("Every report row must contain the same number of cells as the header.", nameof(table));
        if (table.Rows.Count > MaxExportRows)
            throw new ArgumentException($"A report cannot contain more than {MaxExportRows} rows.", nameof(table));

        var normalized = NormalizeFormat(format);
        var safeName = SanitizeFileName(baseFileName);
        return normalized switch
        {
            "csv" => new AnalyticsExportResult(BuildCsv(table), "text/csv; charset=utf-8", safeName + ".csv"),
            "xlsx" => new AnalyticsExportResult(BuildXlsx(table), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", safeName + ".xlsx"),
            "pdf" => new AnalyticsExportResult(BuildPdf(table), "application/pdf", safeName + ".pdf"),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };
    }

    public static string NormalizeFormat(string? format)
    {
        var value = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        return value switch
        {
            "csv" => "csv",
            "xlsx" or "excel" => "xlsx",
            "pdf" => "pdf",
            _ => throw new ArgumentException("Supported export formats: csv, xlsx, pdf.", nameof(format))
        };
    }

    private static byte[] BuildCsv(ReportTable table)
    {
        var builder = new StringBuilder();
        CsvRow(builder, table.Headers.Cast<object?>().ToArray());
        foreach (var row in table.Rows) CsvRow(builder, row.ToArray());
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    private static byte[] BuildXlsx(ReportTable table)
    {
        using var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteZipEntry(archive, "[Content_Types].xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
                  <Default Extension="xml" ContentType="application/xml"/>
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
                  <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
                  <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
                </Types>
                """);
            WriteZipEntry(archive, "_rels/.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
                </Relationships>
                """);
            WriteZipEntry(archive, "xl/workbook.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
                  <sheets><sheet name="Report" sheetId="1" r:id="rId1"/></sheets>
                </workbook>
                """);
            WriteZipEntry(archive, "xl/_rels/workbook.xml.rels", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
                  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
                </Relationships>
                """);
            WriteZipEntry(archive, "xl/styles.xml", """
                <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
                <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
                  <fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><color rgb="FFFFFFFF"/><sz val="11"/><name val="Calibri"/></font></fonts>
                  <fills count="3"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill><fill><patternFill patternType="solid"><fgColor rgb="FF4F46E5"/><bgColor indexed="64"/></patternFill></fill></fills>
                  <borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders>
                  <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
                  <cellXfs count="2"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1" applyAlignment="1"><alignment horizontal="center" vertical="center" wrapText="1"/></xf></cellXfs>
                  <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
                </styleSheet>
                """);
            WriteZipEntry(archive, "xl/worksheets/sheet1.xml", BuildWorksheetXml(table));
        }
        return output.ToArray();
    }

    private static string BuildWorksheetXml(ReportTable table)
    {
        var builder = new StringBuilder();
        var lastColumn = ColumnName(table.Headers.Count);
        var lastRow = table.Rows.Count + 1;
        builder.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        builder.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        builder.Append($"<dimension ref=\"A1:{lastColumn}{lastRow}\"/>");
        builder.Append("<sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews>");
        builder.Append("<cols>");
        for (var index = 0; index < table.Headers.Count; index++)
        {
            var width = Math.Clamp(Math.Max(12, table.Headers[index].Length + 3), 12, 40);
            builder.Append($"<col min=\"{index + 1}\" max=\"{index + 1}\" width=\"{width}\" customWidth=\"1\"/>");
        }
        builder.Append("</cols><sheetData>");
        builder.Append("<row r=\"1\" ht=\"24\" customHeight=\"1\">");
        for (var column = 0; column < table.Headers.Count; column++)
            AppendInlineCell(builder, ColumnName(column + 1) + "1", table.Headers[column], style: 1);
        builder.Append("</row>");

        for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
        {
            var excelRow = rowIndex + 2;
            builder.Append($"<row r=\"{excelRow}\">");
            for (var column = 0; column < table.Headers.Count; column++)
                AppendCell(builder, ColumnName(column + 1) + excelRow, table.Rows[rowIndex][column]);
            builder.Append("</row>");
        }
        builder.Append("</sheetData>");
        builder.Append($"<autoFilter ref=\"A1:{lastColumn}{lastRow}\"/>");
        builder.Append("</worksheet>");
        return builder.ToString();
    }

    private static void AppendCell(StringBuilder builder, string reference, object? value)
    {
        if (value is null)
        {
            AppendInlineCell(builder, reference, string.Empty, 0);
            return;
        }

        if (value is bool boolean)
        {
            builder.Append($"<c r=\"{reference}\" t=\"b\"><v>{(boolean ? 1 : 0)}</v></c>");
            return;
        }

        if (IsNumber(value))
        {
            builder.Append($"<c r=\"{reference}\"><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>");
            return;
        }

        AppendInlineCell(builder, reference, FormatValue(value), 0);
    }

    private static void AppendInlineCell(StringBuilder builder, string reference, string value, int style)
    {
        builder.Append($"<c r=\"{reference}\" t=\"inlineStr\" s=\"{style}\"><is><t xml:space=\"preserve\">{Xml(value)}</t></is></c>");
    }

    private static byte[] BuildPdf(ReportTable table)
    {
        var lines = new List<string>
        {
            table.Title,
            table.Subtitle ?? $"Generated {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm} UTC",
            new string('-', 96),
            string.Join(" | ", table.Headers)
        };
        lines.Add(new string('-', 96));
        foreach (var row in table.Rows)
        {
            var text = string.Join(" | ", row.Select(FormatValue));
            lines.AddRange(WrapPdfLine(text, 105));
        }
        if (table.Rows.Count == 0) lines.Add("No records matched the selected filters.");

        const int linesPerPage = 54;
        var pages = lines.Chunk(linesPerPage).Select(chunk => chunk.ToList()).ToList();
        var objects = new List<byte[]>();
        objects.Add(Ascii("<< /Type /Catalog /Pages 2 0 R >>"));
        var pageIds = Enumerable.Range(0, pages.Count).Select(index => 4 + (index * 2)).ToList();
        objects.Add(Ascii($"<< /Type /Pages /Kids [{string.Join(" ", pageIds.Select(id => $"{id} 0 R"))}] /Count {pages.Count} >>"));
        objects.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));

        for (var index = 0; index < pages.Count; index++)
        {
            var pageId = 4 + (index * 2);
            var contentId = pageId + 1;
            var pageObject = $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 3 0 R >> >> /Contents {contentId} 0 R >>";
            objects.Add(Ascii(pageObject));
            var content = BuildPdfPageContent(pages[index], index + 1, pages.Count);
            var contentBytes = Ascii(content);
            objects.Add(Ascii($"<< /Length {contentBytes.Length} >>\nstream\n").Concat(contentBytes).Concat(Ascii("\nendstream")).ToArray());
        }

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n%????\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{index + 1} 0 obj\n");
            output.Write(objects[index]);
            WriteAscii(output, "\nendobj\n");
        }
        var xref = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count + 1}\n");
        WriteAscii(output, "0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii(output, $"{offset:0000000000} 00000 n \n");
        WriteAscii(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string BuildPdfPageContent(IReadOnlyList<string> lines, int page, int totalPages)
    {
        var builder = new StringBuilder("BT\n/F1 9 Tf\n36 806 Td\n12 TL\n");
        foreach (var line in lines)
        {
            builder.Append('(').Append(PdfEscape(ToPdfAscii(line))).Append(") Tj\nT*\n");
        }
        builder.Append("T*\nT*\n(Page ").Append(page).Append(" of ").Append(totalPages).Append(") Tj\nET");
        return builder.ToString();
    }

    private static IEnumerable<string> WrapPdfLine(string value, int maxLength)
    {
        var text = ToPdfAscii(value);
        if (text.Length <= maxLength) return new[] { text };
        var lines = new List<string>();
        while (text.Length > maxLength)
        {
            var split = text.LastIndexOf(' ', maxLength);
            if (split < maxLength / 2) split = maxLength;
            lines.Add(text[..split].TrimEnd());
            text = text[split..].TrimStart();
        }
        if (text.Length > 0) lines.Add(text);
        return lines;
    }

    private static void WriteZipEntry(ZipArchive archive, string path, string content)
    {
        var entry = archive.CreateEntry(path, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content.Trim());
    }

    private static string ColumnName(int number)
    {
        var result = string.Empty;
        while (number > 0)
        {
            number--;
            result = (char)('A' + number % 26) + result;
            number /= 26;
        }
        return result;
    }

    private static bool IsNumber(object value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;

    private static string FormatValue(object? value) => value switch
    {
        null => string.Empty,
        DateTimeOffset date => date.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture),
        DateTime date => date.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value.ToString() ?? string.Empty
    };

    private static void CsvRow(StringBuilder builder, params object?[] values) => builder.AppendLine(string.Join(',', values.Select(CsvValue)));

    private static string CsvValue(object? value)
    {
        var text = FormatValue(value);
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private static string Xml(string value) => SecurityElement.Escape(value) ?? string.Empty;
    private static string PdfEscape(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    private static string ToPdfAscii(string value) => new(value.Select(character => character is >= ' ' and <= '~' ? character : '?').ToArray());
    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);
    private static void WriteAscii(Stream stream, string value) => stream.Write(Ascii(value));

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var sanitized = new string(value.Select(character => invalid.Contains(character) ? '-' : character).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "civichero-report" : sanitized;
    }
}
