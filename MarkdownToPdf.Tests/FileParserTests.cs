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

}
