using System.Diagnostics;
using System.Text.RegularExpressions;
using Markdig;
using markdown_to_pdf.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using iText.Html2pdf;

namespace markdown_to_pdf.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly string _samplePath;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _samplePath = Path.Combine(env.ContentRootPath, "Samples", "markdown1.txt");
        }

        public IActionResult Index()
        {
            var markdown = System.IO.File.ReadAllText(_samplePath);
            return View(model: markdown);
        }

        [HttpPost]
        public IActionResult GeneratePdf()
        {
            var markdown = System.IO.File.ReadAllText(_samplePath);
            var processed = ReplaceTags(markdown);
            var html = Markdown.ToHtml(processed);
            using var ms = new MemoryStream();
            var props = new ConverterProperties().SetCreateAcroForm(true);
            HtmlConverter.ConvertToPdf(html, ms, props);
            ms.Position = 0;
            return File(ms.ToArray(), "application/pdf", "sample.pdf");
        }

        private static string ReplaceTags(string markdown)
        {
            markdown = Regex.Replace(markdown,
                @"_{2,}\s*<!--\s*\{\{text:(?<name>[^,}]+).*?\}\}\s*-->",
                m => $"<input type=\"text\" name=\"{m.Groups["name"].Value}\" />");

            markdown = Regex.Replace(markdown,
                @"<!--\s*\{\{check:(?<name>[^,}]+).*?\}\}\s*-->",
                m => $"<input type=\"checkbox\" name=\"{m.Groups["name"].Value}\" />");

            markdown = Regex.Replace(markdown,
                @"<!--\s*\{\{radio:(?<name>[^,}]+),group=(?<group>[^,}]+),value=(?<value>[^,}]+).*?\}\}\s*-->",
                m => $"<input type=\"radio\" name=\"{m.Groups["group"].Value}\" value=\"{m.Groups["value"].Value}\" />");

            return markdown;
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
