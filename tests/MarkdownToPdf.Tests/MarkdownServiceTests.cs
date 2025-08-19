using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using MarkdownToPdf.Core.Services;
using Xunit;

namespace MarkdownToPdf.Tests;

public class MarkdownServiceTests
{
    [Fact]
    public void RenderHtml_ReplacesCustomTags_ForPdf()
    {
        var service = new MarkdownService();
        var html = service.RenderHtml("__ <!-- {{text:Name}} -->", true);
        Assert.Contains("<input type=\"text\" name=\"Name\">", html);
    }

    [Fact]
    public void RenderHtml_StripsCustomTags_ForHtml()
    {
        var service = new MarkdownService();
        var html = service.RenderHtml("__ <!-- {{text:Name}} -->", false);
        Assert.DoesNotContain("{{text:Name}}", html);
        Assert.DoesNotContain("<input", html);
    }

    [Fact]
    public async Task GeneratePdf_WritesPdfToStream()
    {
        var service = new MarkdownService();
        await using var ms = new MemoryStream();
        await service.GeneratePdf("test", ms);
        Assert.True(ms.Length > 4);
        var bytes = ms.ToArray();
        Assert.Equal("%PDF", Encoding.ASCII.GetString(bytes, 0, 4));
    }

    [Fact]
    public void RenderHtml_RemovesScriptTags()
    {
        var service = new MarkdownService();
        var html = service.RenderHtml("<script>alert('x')</script>", true);
        Assert.DoesNotContain("<script>", html, StringComparison.OrdinalIgnoreCase);
    }
}
