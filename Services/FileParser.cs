using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
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
        result = Regex.Replace(result, @"\s*\u2022\s*(?=\S)", "\n- ");
        result = Regex.Replace(result, @"\u2022", string.Empty);
        result = ApplyHeadingFormatting(result);
        return result;
    }

    private static string ApplyHeadingFormatting(string text)
    {
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        var headingPattern = new Regex(@"^[A-Z][A-Za-z &'()]+$");
        var first = true;
        List<string[]> table = new();

        void FlushTable()
        {
            if (table.Count == 0) return;
            sb.AppendLine("| " + string.Join(" | ", table[0]) + " |");
            sb.AppendLine("|" + string.Join("|", table[0].Select(_ => " --- ")) + "|");
            for (int i = 1; i < table.Count; i++)
            {
                sb.AppendLine("| " + string.Join(" | ", table[i]) + " |");
            }
            sb.AppendLine();
            table.Clear();
        }

        for (int i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.TrimEnd();
            if (string.IsNullOrWhiteSpace(line))
            {
                FlushTable();
                sb.AppendLine();
                continue;
            }

            if (Regex.IsMatch(line, @"\s{2,}"))
            {
                var cells = Regex.Split(line.Trim(), @"\s{2,}");
                table.Add(cells);
                continue;
            }

            FlushTable();

            if (line.StartsWith("- "))
            {
                sb.AppendLine(line);
                continue;
            }

            var nextLine = i + 1 < lines.Length ? lines[i + 1].Trim() : string.Empty;
            bool nextIsHeadingLike = headingPattern.IsMatch(nextLine);

            if (first)
            {
                sb.AppendLine("# " + line.Trim());
                first = false;
            }
            else if (headingPattern.IsMatch(line.Trim()) && !nextIsHeadingLike)
            {
                sb.AppendLine("## " + line.Trim());
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        FlushTable();
        return sb.ToString();
    }
}

