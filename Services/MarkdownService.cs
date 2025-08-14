using System.Text.RegularExpressions;
using Markdig;
using iText.Html2pdf;
using iText.Kernel.Pdf;

namespace markdown_to_pdf.Services;

public interface IMarkdownService
{
    string RenderHtml(string markdown, bool pdfMode);
    byte[] GeneratePdf(string markdown);
}

public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public string RenderHtml(string markdown, bool pdfMode)
    {
        markdown ??= string.Empty;
        var processed = pdfMode ? ReplaceTags(markdown) : RemoveCustomTags(markdown);
        return Markdown.ToHtml(processed, _pipeline);
    }

    public byte[] GeneratePdf(string markdown)
    {
        var html = RenderHtml(markdown, true);
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        writer.SetCloseStream(false);
        var props = new ConverterProperties().SetCreateAcroForm(true);
        HtmlConverter.ConvertToPdf(html, writer, props);
        return ms.ToArray();
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
}

