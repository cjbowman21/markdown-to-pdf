using System.IO;
using System.Text.RegularExpressions;
using Ganss.Xss;
using Markdig;
using iText.Html2pdf;
using iText.Kernel.Pdf;
using System.Globalization;
using System.Threading.Tasks;

namespace MarkdownToPdf.Core.Services;

public interface IMarkdownService
{
    string RenderHtml(string markdown, bool pdfMode);
    Task GeneratePdf(string markdown, Stream outputStream, Stream? backgroundPdf = null, float offsetLeftPt = 0, float offsetTopPt = 0);
}

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownPipeline _pdfPipeline;
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

        _pdfPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string RenderHtml(string markdown, bool pdfMode)
    {
        markdown ??= string.Empty;
        var processed = pdfMode ? ReplaceTags(markdown) : RemoveCustomTags(markdown);
        var pipeline = pdfMode ? _pdfPipeline : _pipeline;
        var html = Markdown.ToHtml(processed, pipeline);
        return Sanitizer.Sanitize(html);
    }

    public async Task GeneratePdf(string markdown, Stream outputStream, Stream? backgroundPdf = null, float offsetLeftPt = 0, float offsetTopPt = 0)
    {
        var htmlFragment = RenderHtml(markdown, true);
        var css = "html, body { background: transparent !important; }";
        if (Math.Abs(offsetLeftPt) > 0.001f || Math.Abs(offsetTopPt) > 0.001f)
        {
            var left = offsetLeftPt.ToString(CultureInfo.InvariantCulture);
            var top = offsetTopPt.ToString(CultureInfo.InvariantCulture);
            css += $" body {{ margin-left: {left}pt; margin-top: {top}pt; }}";
        }
        var html = $"<html><head><meta charset=\"utf-8\"/><style>{css}</style></head><body>{htmlFragment}</body></html>";

        // If no background is provided, we still run through the same pipeline
        // so margin-based offsets apply consistently.

        // Convert HTML to a temporary PDF in-memory
        using var generatedPdfStream = new MemoryStream();
        using (var tempWriter = new PdfWriter(generatedPdfStream))
        {
            tempWriter.SetCloseStream(false);
            var props = new ConverterProperties().SetCreateAcroForm(true);
            HtmlConverter.ConvertToPdf(html, tempWriter, props);
        }

        // Prepare background stream (copy to memory to ensure seekability)
        MemoryStream? backgroundBuffer = null;
        if (backgroundPdf is not null)
        {
            backgroundBuffer = new MemoryStream();
            await backgroundPdf.CopyToAsync(backgroundBuffer);
            backgroundBuffer.Position = 0;
        }

        // Open documents for overlay
        generatedPdfStream.Position = 0;
        using var genDoc = new PdfDocument(new PdfReader(generatedPdfStream));
        using var bgDoc = backgroundBuffer is null ? null : new PdfDocument(new PdfReader(backgroundBuffer));
        using var outWriter = new PdfWriter(outputStream);
        outWriter.SetCloseStream(false);
        using var outDoc = new PdfDocument(outWriter);

        var genPages = genDoc.GetNumberOfPages();
        var bgPages = bgDoc?.GetNumberOfPages() ?? 0;

        for (int i = 1; i <= genPages; i++)
        {
            var genPage = genDoc.GetPage(i);
            var outPage = outDoc.AddNewPage(new iText.Kernel.Geom.PageSize(genPage.GetPageSize()));
            var canvas = new iText.Kernel.Pdf.Canvas.PdfCanvas(outPage);

            if (bgPages > 0 && bgDoc is not null)
            {
                int bgIndex = bgPages == 1 ? 1 : Math.Min(i, bgPages);
                var bgPage = bgDoc.GetPage(bgIndex);
                var bgSize = bgPage.GetPageSize();
                var genSize = genPage.GetPageSize();
                var bgXObj = bgPage.CopyAsFormXObject(outDoc);

                // Scale background to generated page size if needed
                float scaleX = genSize.GetWidth() / bgSize.GetWidth();
                float scaleY = genSize.GetHeight() / bgSize.GetHeight();
                if (Math.Abs(scaleX - 1f) > 0.001f || Math.Abs(scaleY - 1f) > 0.001f)
                {
                    canvas.SaveState();
                    canvas.ConcatMatrix(scaleX, 0, 0, scaleY, 0, 0);
                    canvas.AddXObjectAt(bgXObj, 0, 0);
                    canvas.RestoreState();
                }
                else
                {
                    canvas.AddXObjectAt(bgXObj, 0, 0);
                }
            }

            var genXObj = genPage.CopyAsFormXObject(outDoc);
            // Content was already offset via CSS margins; draw at (0,0)
            canvas.AddXObjectAt(genXObj, 0, 0);
        }

        await outputStream.FlushAsync();
    }

    private static string ReplaceTags(string markdown)
    {
        markdown = Regex.Replace(markdown,
            @"_{2,}\s*<!--\s*\{\{text:(?<name>[^,}]+).*?\}\}\s*-->",
            m => $"<input type=\"text\" name=\"{m.Groups["name"].Value}\" />");

        markdown = Regex.Replace(markdown,
            @"\[\s+]\s*<!--\s*\{\{check:(?<name>[^,}]+).*?\}\}\s*-->",
            m => $"<input type=\"checkbox\" name=\"{m.Groups["name"].Value}\" />");

        markdown = Regex.Replace(markdown,
            @"\(\s+\)\s*<!--\s*\{\{radio:(?<name>[^,}]+),group=(?<group>[^,}]+),value=(?<value>[^,}]+).*?\}\}\s*-->",
            m => $"<input type=\"radio\" name=\"{m.Groups["group"].Value}\" value=\"{m.Groups["value"].Value}\" />");

        markdown = Regex.Replace(markdown,
            @"\s*<!--\s*\{\{\s*pagebreak\s*\}\}\s*-->\s*",
            "\n<div style=\"page-break-after: always;\"></div>\n",
            RegexOptions.IgnoreCase);

        return markdown;
    }

    private static string RemoveCustomTags(string markdown)
    {
        return Regex.Replace(markdown, @"<!--\s*\{\{.*?\}\}\s*-->", string.Empty);
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Add("input");
        sanitizer.AllowedTags.Add("div");
        sanitizer.AllowedAttributes.Add("type");
        sanitizer.AllowedAttributes.Add("name");
        sanitizer.AllowedAttributes.Add("value");
        sanitizer.AllowedAttributes.Add("style");
        return sanitizer;
    }
}

