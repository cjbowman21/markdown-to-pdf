using System.IO;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using markdown_to_pdf.Services;
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

}
