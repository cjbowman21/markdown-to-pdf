using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using markdown_to_pdf.Services;
using UglyToad.PdfPig.Writer;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using Xunit;

namespace MarkdownToPdf.Tests;

public class FileParserTests
{
    [Fact]
    public async Task ParseToMarkdownAsync_ReadsText()
    {
        var parser = new FileParser();
        await using var ms = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        var markdown = await parser.ParseToMarkdownAsync(ms, ".txt");
        Assert.Equal("hello", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_ReadsDocx()
    {
        var parser = new FileParser();
        await using var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
        {
            doc.AddMainDocumentPart();
            doc.MainDocumentPart!.Document = new Document(
                new Body(new Paragraph(new Run(new Text("hello")))));
            var stylePart = doc.MainDocumentPart.AddNewPart<StyleDefinitionsPart>();
            stylePart.Styles = new Styles(new Style(
                new StyleName { Val = "Normal" },
                new StyleId { Val = "Normal" },
                new PrimaryStyle()) { Type = StyleValues.Paragraph });
            stylePart.Styles.Save();
            var settingsPart = doc.MainDocumentPart.AddNewPart<DocumentSettingsPart>();
            settingsPart.Settings = new Settings(new Compatibility());
        }
        ms.Position = 0;
        var markdown = await parser.ParseToMarkdownAsync(ms, ".docx");
        Assert.Contains("hello", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_ReadsPdf()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Hello", 12, new PdfPoint(25, 800), font);
        page.AddText("\u2022 World", 12, new PdfPoint(25, 780), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("Hello", markdown);
        Assert.Contains("- World", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_SplitsInlinePdfBullets()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Item1 \u2022 Item2 \u2022 Item3", 12, new PdfPoint(25, 800), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("Item1", markdown);
        Assert.Contains("- Item2", markdown);
        Assert.Contains("- Item3", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_AddsHeadings()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Heading One", 12, new PdfPoint(25, 800), font);
        page.AddText("Body text.", 12, new PdfPoint(25, 780), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("# Heading One", markdown);
        Assert.Contains("Body text.", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_ConvertsPdfTable()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Col1   Col2", 12, new PdfPoint(25, 800), font);
        page.AddText("A1   B1", 12, new PdfPoint(25, 780), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("| Col1 | Col2 |", markdown);
        Assert.Contains("| A1 | B1 |", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_ConvertsLargeStandaloneNumbers()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("Question", 12, new PdfPoint(25, 800), font);
        page.AddText("123456789012", 12, new PdfPoint(25, 780), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("Question", markdown);
        Assert.Contains("____________", markdown);
        Assert.DoesNotContain("123456789012", markdown);
    }

    [Fact]
    public async Task ParseToMarkdownAsync_StripsLongNumbersAdjacentToText()
    {
        var parser = new FileParser();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842);
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);
        page.AddText("123456789012Question", 12, new PdfPoint(25, 800), font);
        var pdfBytes = builder.Build();
        await using var ms = new MemoryStream(pdfBytes);
        var markdown = await parser.ParseToMarkdownAsync(ms, ".pdf");
        Assert.Contains("Question", markdown);
        Assert.DoesNotContain("123456789012", markdown);
    }
}
