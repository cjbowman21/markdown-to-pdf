using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using MarkdownToPdf.Core.Services;
using Xunit;

namespace MarkdownToPdf.Tests;

public class PdfGenerationTests
{
    [Fact]
    public async Task GeneratePdf_BasicMarkdown_ContainsRenderedText()
    {
        var service = new MarkdownService();
        await using var ms = new MemoryStream();
        await service.GeneratePdf("# Hello", ms);
        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(ms.ToArray())));
        var text = PdfTextExtractor.GetTextFromPage(pdf.GetPage(1), new LocationTextExtractionStrategy());
        Assert.Contains("Hello", text);
    }

    [Fact]
    public void RenderHtml_ReplacesPagebreakSentinel()
    {
        var service = new MarkdownService();
        var html = service.RenderHtml("First\n<!-- {{pagebreak}} -->\nSecond", true);
        Assert.Contains("<div style=\"page-break-after: always", html);
    }
}
