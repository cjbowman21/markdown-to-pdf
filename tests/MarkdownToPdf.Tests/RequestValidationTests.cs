using System.Reflection;
using MarkdownToPdf.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using Xunit;

namespace MarkdownToPdf.Tests;

public class RequestValidationTests
{
    [Fact]
    public void GeneratePdf_HasRequestSizeLimit()
    {
        var method = typeof(HomeController).GetMethod("GeneratePdf");
        var attr = method!.GetCustomAttribute<RequestSizeLimitAttribute>();
        Assert.NotNull(attr);
        var metadata = (IRequestSizeLimitMetadata)attr!;
        Assert.Equal(10 * 1024 * 1024, metadata.MaxRequestBodySize);
    }
}
