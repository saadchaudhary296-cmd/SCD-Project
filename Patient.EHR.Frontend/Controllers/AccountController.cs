using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Patient.EHR.Data;
using Patient.EHR.Models;
using System.ComponentModel.DataAnnotations;

namespace Patient.EHR.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccountController> _logger;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

        public AccountController(
            IWebHostEnvironment environment,
            ApplicationDbContext db,
            ILogger<AccountController> logger)
        {
            _environment = environment;
            _db = db;
            _logger = logger;
        }

        public IActionResult Login()
        {
            return HtmlPage("Account", "Login.html");
        }

        public IActionResult Signup()
        {
            return HtmlPage("Account", "Signup.html");
        }

        public IActionResult Dashboard()
        {
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(userEmail))
            {
                return RedirectToAction("Login");
            }

            return HtmlPage("Account", "Dashboard.html");
        }
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Signup(
            string? fullName,
            string? email,
            string? password,
            string? confirmPassword,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fullName) ||
                fullName.Length > 150 ||
                string.IsNullOrWhiteSpace(email) ||
                !new EmailAddressAttribute().IsValid(email) ||
                email.Length > 254 ||
                string.IsNullOrWhiteSpace(password) ||
                password != confirmPassword ||
                !IsStrongPassword(password))
            {
                return RedirectToAction(nameof(Signup), new { error = "Enter a valid name, email, and password." });
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            if (await _db.Users.AnyAsync(user => user.Email == normalizedEmail, cancellationToken))
            {
                return RedirectToAction(nameof(Signup), new { error = "An account with this email already exists." });
            }

            var user = new ApplicationUser
            {
                FullName = fullName.Trim(),
                Email = normalizedEmail
            };
            user.PasswordHash = _passwordHasher.HashPassword(user, password);

            _db.Users.Add(user);
            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(exception, "Could not save the new account.");
                return RedirectToAction(nameof(Signup), new { error = "Signup failed. Please try again." });
            }

            return RedirectToAction(nameof(Login), new { error = "Account created. Please sign in." });
        }

        [HttpPost]
        public async Task<IActionResult> Login(
            string? email,
            string? password,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return RedirectToAction(nameof(Login), new { error = "Invalid Credentials" });
            }

            var normalizedEmail = email.Trim().ToLowerInvariant();
            var user = await _db.Users
                .SingleOrDefaultAsync(
                    account => account.Email == normalizedEmail && account.IsActive,
                    cancellationToken);

            if (user is not null &&
                _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password) !=
                PasswordVerificationResult.Failed)
            {
                HttpContext.Session.SetString("UserEmail", user.Email);
                return RedirectToAction("Dashboard");
            }

            return RedirectToAction(nameof(Login), new { error = "Invalid Credentials" });
        }

        private PhysicalFileResult HtmlPage(string directory, string fileName)
        {
            var path = Path.Combine(_environment.ContentRootPath, "Views", directory, fileName);
            return PhysicalFile(path, "text/html; charset=utf-8");
        }

        private static bool IsStrongPassword(string password)
        {
            return password.Length >= 8 &&
                password.Length <= 256 &&
                password.Any(char.IsLower) &&
                password.Any(char.IsUpper) &&
                password.Any(char.IsDigit) &&
                password.Any(character => !char.IsLetterOrDigit(character));
        }
    }
}