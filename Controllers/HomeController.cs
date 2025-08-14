using System.Diagnostics;
using markdown_to_pdf.Models;
using markdown_to_pdf.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;

namespace markdown_to_pdf.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMarkdownService _markdownService;
        private readonly string _samplePath;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, IMarkdownService markdownService)
        {
            _logger = logger;
            _markdownService = markdownService;
            _samplePath = Path.Combine(env.ContentRootPath, "Samples", "markdown1.txt");
        }

        public IActionResult Index()
        {
            var markdown = System.IO.File.ReadAllText(_samplePath);
            return View(model: markdown);
        }

        [HttpPost]
        public IActionResult GeneratePdf(string? markdown)
        {
            markdown ??= System.IO.File.ReadAllText(_samplePath);
            var pdf = _markdownService.GeneratePdf(markdown);
            return File(pdf, "application/pdf", "sample.pdf");
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
