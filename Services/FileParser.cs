using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using OpenXmlPowerTools;
using ReverseMarkdown;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using System.Xml.Linq;

namespace markdown_to_pdf.Services;

public interface IFileParser
{
    Task<string> ParseToMarkdownAsync(Stream file, string extension);
}

public class FileParser : IFileParser
{
    public async Task<string> ParseToMarkdownAsync(Stream file, string extension)
    {
        extension = extension.ToLowerInvariant();
        return extension switch
        {
            ".txt" => await ParseTextAsync(file),
            ".doc" or ".docx" => await ParseDocxAsync(file),
            ".pdf" => await ParsePdfAsync(file),
            _ => throw new InvalidOperationException("Unsupported file type."),
        };
    }

    private static async Task<string> ParseTextAsync(Stream file)
    {
        using var reader = new StreamReader(file, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ParseDocxAsync(Stream file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        using var doc = WordprocessingDocument.Open(ms, true);
        var settings = new HtmlConverterSettings
        {
            PageTitle = "",
            FabricateCssClasses = false,
            ImageHandler = null
        };
        var html = HtmlConverter.ConvertToHtml(doc, settings);
        var htmlString = html.ToString(SaveOptions.DisableFormatting);
        htmlString = Regex.Replace(htmlString, "<img[^>]*>", string.Empty, RegexOptions.IgnoreCase);
        var converter = new Converter(new Config { GithubFlavored = true });
        return converter.Convert(htmlString);
    }

    private static async Task<string> ParsePdfAsync(Stream file)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        ms.Position = 0;
        using var pdf = PdfDocument.Open(ms);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
        {
            var text = ContentOrderTextExtractor.GetText(page);
            sb.AppendLine(text);
            sb.AppendLine();
        }

        var result = sb.ToString();
        result = Regex.Replace(result, @"^\s*\u2022\s*", "- ", RegexOptions.Multiline);
        return result;
    }
}

