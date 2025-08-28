using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using MarkdownToPdf.Web.Models;
using MarkdownToPdf.Core.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace MarkdownToPdf.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMarkdownService _markdownService;
        private readonly IFileParser _fileParser;
        private readonly string _samplePath;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, IMarkdownService markdownService, IFileParser fileParser)
        {
            _logger = logger;
            _markdownService = markdownService;
            _fileParser = fileParser;
            _samplePath = Path.Combine(env.ContentRootPath, "Samples", "markdown1.txt");
        }

        public IActionResult Index()
        {
            string markdown;
            if (System.IO.File.Exists(_samplePath))
            {
                markdown = System.IO.File.ReadAllText(_samplePath);
            }
            else
            {
                markdown = "Sample markdown file not found.";
            }

            ViewData["Title"] = "Markdown to PDF Converter";
            ViewData["Description"] = "Free, fast Markdown to PDF converter with live preview. Paste Markdown or upload a file and generate a clean PDF.";

            return View(model: markdown);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> GeneratePdf(string? markdown, string? fileName, IFormFile? letterheadPdf, bool applyOverlay = false, float? offsetX = null, float? offsetY = null)
        {
            if (markdown == null)
            {
                await using var fs = new FileStream(_samplePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var reader = new StreamReader(fs);
                markdown = await reader.ReadToEndAsync();
            }

            // If a letterhead/background PDF is provided, buffer it so the async writer can use it safely
            byte[]? backgroundBytes = null;
            if (letterheadPdf is not null && letterheadPdf.Length > 0)
            {
                await using var bgStream = letterheadPdf.OpenReadStream();
                using var ms = new MemoryStream();
                await bgStream.CopyToAsync(ms);
                backgroundBytes = ms.ToArray();
            }

            var pipe = new Pipe();
            var left = offsetX ?? 0f;
            var top = offsetY ?? 0f;
            _ = Task.Run(async () =>
            {
                await using var writerStream = pipe.Writer.AsStream();
                if (backgroundBytes is not null && applyOverlay)
                {
                    using var bgMs = new MemoryStream(backgroundBytes);
                    await _markdownService.GeneratePdf(markdown!, writerStream, bgMs, left, top);
                }
                else
                {
                    await _markdownService.GeneratePdf(markdown!, writerStream, null, left, top);
                }
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> PreviewPdf(string? markdown, IFormFile? letterheadPdf, bool applyOverlay = false, float? offsetX = null, float? offsetY = null)
        {
            if (markdown == null)
            {
                await using var fs = new FileStream(_samplePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
                using var reader = new StreamReader(fs);
                markdown = await reader.ReadToEndAsync();
            }

            byte[]? backgroundBytes = null;
            if (letterheadPdf is not null && letterheadPdf.Length > 0)
            {
                await using var bgStream = letterheadPdf.OpenReadStream();
                using var ms = new MemoryStream();
                await bgStream.CopyToAsync(ms);
                backgroundBytes = ms.ToArray();
            }

            var left = offsetX ?? 0f;
            var top = offsetY ?? 0f;
            var pipe = new Pipe();
            _ = Task.Run(async () =>
            {
                await using var writerStream = pipe.Writer.AsStream();
                if (applyOverlay && backgroundBytes is not null)
                {
                    using var bgMs = new MemoryStream(backgroundBytes);
                    await _markdownService.GeneratePdf(markdown!, writerStream, bgMs, left, top);
                }
                else
                {
                    await _markdownService.GeneratePdf(markdown!, writerStream, null, left, top);
                }
                await pipe.Writer.CompleteAsync();
            });

            // Allow embedding this PDF in an iframe on same-origin
            Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
            Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self'";

            return new FileStreamResult(pipe.Reader.AsStream(), "application/pdf");
        }

        [HttpPost]
        [RequestSizeLimit(2 * 1024 * 1024)]
        public async Task<IActionResult> UploadFile(IFormFile upload)
        {
            if (upload == null || upload.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded." });
            }

            try
            {
                await using var stream = upload.OpenReadStream();
                var markdown = await _fileParser.ParseToMarkdownAsync(stream, Path.GetExtension(upload.FileName));

                var wordCount = Regex.Matches(markdown, "\\b\\w+\\b").Count;
                var headingCount = Regex.Matches(markdown, "^#{1,6}\\s", RegexOptions.Multiline).Count;
                var listItemCount = Regex.Matches(markdown, "^\\s*(?:[-*+]|\\d+\\.)\\s", RegexOptions.Multiline).Count;
                var checkboxCount = Regex.Matches(markdown, "^\\s*[-*+]\\s+\\[[ xX]\\]\\s", RegexOptions.Multiline).Count;

                return Ok(new
                {
                    markdown,
                    details = new
                    {
                        fileName = upload.FileName,
                        wordCount,
                        headingCount,
                        listItemCount,
                        checkboxCount
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
