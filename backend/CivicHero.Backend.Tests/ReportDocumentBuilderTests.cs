using System.IO.Compression;
using System.Text;
using CivicHero.Backend.Core.Services;

namespace CivicHero.Backend.Tests;

public sealed class ReportDocumentBuilderTests
{
    private static readonly ReportTable Sample = new(
        "Sample report",
        ["Id", "Name", "Created"],
        [
            [1, "First", new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero)],
            [2, "Second", null]
        ]);

    [Fact]
    public void Csv_export_should_include_utf8_bom_and_headers()
    {
        var result = ReportDocumentBuilder.Build(Sample, "csv", "sample");
        result.ContentType.Should().StartWith("text/csv");
        result.FileName.Should().EndWith(".csv");
        result.Content.Take(3).Should().Equal(Encoding.UTF8.GetPreamble());
        Encoding.UTF8.GetString(result.Content).Should().Contain("\"Id\",\"Name\",\"Created\"");
    }

    [Fact]
    public void Excel_export_should_create_a_minimal_openxml_workbook()
    {
        var result = ReportDocumentBuilder.Build(Sample, "xlsx", "sample");
        result.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        using var archive = new ZipArchive(new MemoryStream(result.Content), ZipArchiveMode.Read);
        archive.GetEntry("xl/workbook.xml").Should().NotBeNull();
        archive.GetEntry("xl/worksheets/sheet1.xml").Should().NotBeNull();
        archive.GetEntry("xl/styles.xml").Should().NotBeNull();
    }

    [Fact]
    public void Pdf_export_should_create_a_pdf_document()
    {
        var result = ReportDocumentBuilder.Build(Sample, "pdf", "sample");
        result.ContentType.Should().Be("application/pdf");
        result.FileName.Should().EndWith(".pdf");
        Encoding.ASCII.GetString(result.Content, 0, 8).Should().StartWith("%PDF-1.4");
        Encoding.ASCII.GetString(result.Content).Should().Contain("xref").And.Contain("%%EOF");
    }
}
