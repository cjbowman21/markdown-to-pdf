using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace MarkdownToPdf.Web.Controllers
{
    [ApiController]
    public class SitemapController : ControllerBase
    {
        [HttpGet]
        [Route("sitemap.xml")]
        public IActionResult Get()
        {
            var req = HttpContext.Request;
            var baseUrl = $"{req.Scheme}://{req.Host}";

            var urls = new[]
            {
                new { Loc = $"{baseUrl}/", Priority = "1.0" },
                new { Loc = $"{baseUrl}/Home/Privacy", Priority = "0.3" }
            };

            var lastMod = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
            foreach (var u in urls)
            {
                sb.AppendLine("  <url>");
                sb.AppendLine($"    <loc>{System.Security.SecurityElement.Escape(u.Loc)}</loc>");
                sb.AppendLine($"    <lastmod>{lastMod}</lastmod>");
                sb.AppendLine($"    <priority>{u.Priority}</priority>");
                sb.AppendLine("  </url>");
            }
            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}

