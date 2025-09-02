using System.IO;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using MarkdownToPdf.Core.Services;
using iText.Forms;
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

    [Fact]
    public async Task GeneratePdf_WithTextField_PreservesAcroForm()
    {
        var service = new MarkdownService();
        await using var output = new MemoryStream();

        await using var bg = new MemoryStream();
        using (var writer = new PdfWriter(bg))
        {
            writer.SetCloseStream(false);
            using var doc = new PdfDocument(writer);
            doc.AddNewPage();
        }
        bg.Position = 0;

        await service.GeneratePdf("__ <!-- {{text:Name}} -->", output, bg);

        using var pdf = new PdfDocument(new PdfReader(new MemoryStream(output.ToArray())));
        var form = PdfAcroForm.GetAcroForm(pdf, false);
        Assert.NotNull(form);
        Assert.NotNull(form.GetField("Name"));
    }
}
