using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

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
        using var doc = WordprocessingDocument.Open(ms, false);
        var sb = new StringBuilder();
        var body = doc.MainDocumentPart!.Document.Body;
        foreach (var para in body.Elements<Paragraph>())
        {
            var text = para.InnerText ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                sb.AppendLine();
                continue;
            }

            var styleId = para.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
            if (styleId != null && styleId.StartsWith("Heading", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(styleId.Substring("Heading".Length), out var level))
                {
                    level = 1;
                }
                level = Math.Clamp(level, 1, 6);
                sb.AppendLine(new string('#', level) + " " + text);
            }
            else if (para.ParagraphProperties?.NumberingProperties != null)
            {
                sb.AppendLine("- " + text);
            }
            else
            {
                sb.AppendLine(text);
            }
        }

        return sb.ToString();
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
        result = Regex.Replace(result, @"\d{9,}", string.Empty);
        result = Regex.Replace(result, @"^\s*☐+\s*", string.Empty, RegexOptions.Multiline);
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

            if (Regex.IsMatch(line.Trim(), @"^\d{9,}$"))
            {
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

