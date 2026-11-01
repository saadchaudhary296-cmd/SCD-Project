using Microsoft.AspNetCore.Mvc;

namespace Patient.EHR.Controllers
{
    public class PatientController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public PatientController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            var path = Path.Combine(_environment.ContentRootPath, "Views", "Patient", "Index.html");
            return PhysicalFile(path, "text/html; charset=utf-8");
        }
    }
}