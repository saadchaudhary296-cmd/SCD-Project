using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace Patient.EHR.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebHostEnvironment _environment;

        public HomeController(ILogger<HomeController> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var path = Path.Combine(_environment.ContentRootPath, "Views", "Shared", "Error.html");
            return PhysicalFile(path, "text/html; charset=utf-8");
        }
    }
}
