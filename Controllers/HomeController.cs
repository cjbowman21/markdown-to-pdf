using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
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
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> GeneratePdf(string? markdown, string? fileName)
        {
            if (markdown == null)
            {
                await using var fs = new FileStream(_samplePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var reader = new StreamReader(fs);
                markdown = await reader.ReadToEndAsync();
            }

            var pipe = new Pipe();
            _ = Task.Run(async () =>
            {
                await using var writerStream = pipe.Writer.AsStream();
                await _markdownService.GeneratePdf(markdown!, writerStream);
                await pipe.Writer.CompleteAsync();
            });

            var downloadName = string.IsNullOrWhiteSpace(fileName)
                ? "document"
                : Path.GetFileNameWithoutExtension(fileName);

            return new FileStreamResult(pipe.Reader.AsStream(), "application/pdf")
            {
                FileDownloadName = $"{downloadName}.pdf"
            };
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
