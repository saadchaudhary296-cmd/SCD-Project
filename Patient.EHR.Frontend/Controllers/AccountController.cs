using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Patient.EHR.Data;
using Patient.EHR.Models;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;

namespace Patient.EHR.Controllers
{
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDataProtector _resetTokenProtector;
        private readonly PasswordHasher<ApplicationUser> _passwordHasher = new();

        public AccountController(
            IWebHostEnvironment environment,
            ApplicationDbContext db,
            ILogger<AccountController> logger,
            IConfiguration configuration,
            IDataProtectionProvider dataProtectionProvider)
        {
            _environment = environment;
            _db = db;
            _logger = logger;
            _configuration = configuration;
            _resetTokenProtector = dataProtectionProvider.CreateProtector("Patient.EHR.PasswordReset.v1");
        }

        public IActionResult Login()
        {
            return HtmlPage("Account", "Login.html");
        }

        public IActionResult Signup()
        {
            return HtmlPage("Account", "Signup.html");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            return HtmlPage("Account", "ResetPassword.html");
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
                fullName.Trim().Any(character => !char.IsLetter(character) && character != ' ') ||
                string.IsNullOrWhiteSpace(email) ||
                !new EmailAddressAttribute().IsValid(email) ||
                email.Length > 254 ||
                string.IsNullOrWhiteSpace(password) ||
                password != confirmPassword ||
                !IsStrongPassword(password))
            {
                return RedirectToAction(nameof(Signup), new
                {
                    error = "Enter a name using letters and spaces only, a valid email, and a valid password."
                });
            }

            if (!email.Trim().EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction(nameof(Signup), new
                {
                    error = "Use a Gmail address ending in @gmail.com."
                });
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

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(
            [FromBody] ForgotPasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Email) ||
                request.Email.Length > 254 ||
                !new EmailAddressAttribute().IsValid(request.Email))
            {
                return BadRequest(new { message = "Enter a valid email address." });
            }

            var smtpHost = _configuration["Email:Smtp:Host"];
            var fromAddress = _configuration["Email:Smtp:FromAddress"];
            var publicBaseUrl = _configuration["Email:PublicBaseUrl"];
            if (string.IsNullOrWhiteSpace(smtpHost) ||
                string.IsNullOrWhiteSpace(fromAddress) ||
                string.IsNullOrWhiteSpace(publicBaseUrl) ||
                !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps &&
                    !(_environment.IsDevelopment() && baseUri.Scheme == Uri.UriSchemeHttp)))
            {
                _logger.LogError("Password reset email is not configured correctly.");
                return StatusCode(503, new
                {
                    message = "Password reset is temporarily unavailable. Please try again later."
                });
            }

            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.SingleOrDefaultAsync(
                account => account.Email == normalizedEmail && account.IsActive,
                cancellationToken);

            if (user is not null)
            {
                var tokenPayload = string.Join(
                    "\n",
                    user.Email,
                    user.PasswordHash,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(
                        System.Globalization.CultureInfo.InvariantCulture));
                var token = _resetTokenProtector.Protect(tokenPayload);
                var resetUrl = new Uri(
                    baseUri,
                    $"Account/ResetPassword?token={Uri.EscapeDataString(token)}");

                try
                {
                    await SendPasswordResetEmailAsync(
                        smtpHost,
                        fromAddress,
                        user.Email,
                        resetUrl,
                        cancellationToken);
                }
                catch (Exception exception) when (
                    exception is SmtpException or InvalidOperationException or FormatException)
                {
                    _logger.LogError(exception, "Could not send a password reset email.");
                    return StatusCode(503, new
                    {
                        message = "We couldn't send the reset email right now. Please try again later."
                    });
                }
            }

            return Ok(new
            {
                message = "If an active account uses that email, a password reset link will be sent shortly."
            });
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(
            [FromBody] ResetPasswordRequest request,
            CancellationToken cancellationToken)
        {
            if (request is null ||
                string.IsNullOrWhiteSpace(request.Token) ||
                string.IsNullOrWhiteSpace(request.Password) ||
                request.Password != request.ConfirmPassword ||
                !IsStrongPassword(request.Password))
            {
                return BadRequest(new
                {
                    message = "Enter matching passwords with at least 8 characters, including an uppercase letter, a lowercase letter, a number, and a symbol."
                });
            }

            string[] tokenParts;
            try
            {
                tokenParts = _resetTokenProtector.Unprotect(request.Token).Split('\n');
            }
            catch (CryptographicException)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired. Request a new one." });
            }

            if (tokenParts.Length != 3 ||
                !long.TryParse(
                    tokenParts[2],
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var issuedAt))
            {
                return BadRequest(new { message = "This reset link is invalid or has expired. Request a new one." });
            }

            var issuedAtUtc = DateTimeOffset.FromUnixTimeSeconds(issuedAt);
            var now = DateTimeOffset.UtcNow;
            if (issuedAtUtc > now || now - issuedAtUtc > TimeSpan.FromMinutes(30))
            {
                return BadRequest(new { message = "This reset link is invalid or has expired. Request a new one." });
            }

            var user = await _db.Users.SingleOrDefaultAsync(
                account => account.Email == tokenParts[0] &&
                    account.IsActive &&
                    account.PasswordHash == tokenParts[1],
                cancellationToken);
            if (user is null)
            {
                return BadRequest(new { message = "This reset link is invalid or has expired. Request a new one." });
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "Your password has been reset. You can now sign in." });
        }

        private async Task SendPasswordResetEmailAsync(
            string smtpHost,
            string fromAddress,
            string recipient,
            Uri resetUrl,
            CancellationToken cancellationToken)
        {
            var port = _configuration.GetValue<int?>("Email:Smtp:Port") ?? 587;
            var enableSsl = _configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true;
            var username = _configuration["Email:Smtp:Username"];
            var password = _configuration["Email:Smtp:Password"];
            if (port is < 1 or > 65535)
            {
                throw new InvalidOperationException("SMTP port must be between 1 and 65535.");
            }

            if (string.IsNullOrWhiteSpace(username) != string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Configure both SMTP username and password, or neither.");
            }

            using var message = new MailMessage(fromAddress, recipient)
            {
                Subject = "Reset your EHR password",
                Body = $"Use this secure link to reset your EHR password. It expires in 30 minutes:\n\n{resetUrl}\n\nIf you did not request a password reset, you can ignore this email.",
                IsBodyHtml = false
            };
            using var client = new SmtpClient(smtpHost, port)
            {
                EnableSsl = enableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                if (string.IsNullOrEmpty(password))
                {
                    throw new InvalidOperationException("SMTP password is required when an SMTP username is configured.");
                }

                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(username, password);
            }

            await client.SendMailAsync(message, cancellationToken);
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

    public sealed class ForgotPasswordRequest
    {
        public string? Email { get; set; }
    }

    public sealed class ResetPasswordRequest
    {
        public string? Token { get; set; }
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
    }
}