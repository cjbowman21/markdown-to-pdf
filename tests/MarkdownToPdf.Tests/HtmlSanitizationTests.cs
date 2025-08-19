using MarkdownToPdf.Core.Services;
using Xunit;

namespace MarkdownToPdf.Tests;

public class HtmlSanitizationTests
{
    [Fact]
    public void RenderHtml_DisabledHtml_EncodesTags()
    {
        var service = new MarkdownService();
        var html = service.RenderHtml("<b>bold</b>", false);
        Assert.DoesNotContain("<b>bold</b>", html, System.StringComparison.OrdinalIgnoreCase);
        Assert.Contains("&lt;b&gt;bold&lt;/b&gt;", html, System.StringComparison.OrdinalIgnoreCase);
    }
}
