using System;
using System.Text;
using markdown_to_pdf.Services;
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
    public void GeneratePdf_ReturnsPdfBytes()
    {
        var service = new MarkdownService();
        var bytes = service.GeneratePdf("test");
        Assert.True(bytes.Length > 4);
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
