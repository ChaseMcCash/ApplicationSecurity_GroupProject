// 
// FILE          : AccountController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the AccountController responsible for user 
//   authentication operations: registration, login, and logout.
//   It uses ASP.NET Identity for secure password hashing (BCrypt) 
//   and session management. All login attempts (success and failure) 
//   are recorded via the AuditService for OWASP A09 compliance.
//   Rate limiting is applied to the login endpoint (OWASP A07).
// 

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models;
using StudentHubForum.Models.ViewModels;
using StudentHubForum.Services;
using System.Security.Claims;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Handles user registration, login, and logout.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IAuditService _auditService;

        //
        // FUNCTION    : AccountController (constructor)
        // DESCRIPTION : Injects Identity managers and the audit service.
        // PARAMETERS  : UserManager<ApplicationUser> userManager   : manages user accounts
        //               SignInManager<ApplicationUser> signInManager : manages sign-in operations
        //               IAuditService auditService                   : logs security events
        // RETURNS     : N/A (constructor)
        //
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        // Avatar: only image types allowed
        private static readonly HashSet<string> AvatarContentTypes = new()
        {
            "image/jpeg", "image/png", "image/gif", "image/webp"
        };
        private static readonly string[] AvatarExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxAvatarSizeBytes = 2 * 1024 * 1024; // 2 MB

        //
        // FUNCTION    : AccountController (constructor)
        // DESCRIPTION : Injects Identity managers, audit service, db context, and hosting environment.
        // PARAMETERS  : UserManager<ApplicationUser> userManager   : manages user accounts
        //               SignInManager<ApplicationUser> signInManager : manages sign-in operations
        //               IAuditService auditService                   : logs security events
        //               ApplicationDbContext context                 : the EF Core database context
        //               IWebHostEnvironment environment              : provides web root path
        // RETURNS     : N/A (constructor)
        //
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditService auditService,
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditService = auditService;
            _context = context;
            _environment = environment;
        }

        // ---------------------------------------------------------------
        //  REGISTER
        // ---------------------------------------------------------------

        //
        // FUNCTION    : Register (GET)
        // DESCRIPTION : Displays the user registration form.
        // PARAMETERS  : none
        // RETURNS     : IActionResult : the Register view
        //
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        //
        // FUNCTION    : Register (POST)
        // DESCRIPTION : Processes the registration form, creates a new user 
        //               with the "Student" role, and logs the event.
        // PARAMETERS  : RegisterViewModel model : the form data
        // RETURNS     : Task<IActionResult> : redirects to Home on success, 
        //               or returns the form with errors
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Assign the default "Student" role to all new registrations
                await _userManager.AddToRoleAsync(user, "Student");

                // Log successful registration
                await _auditService.LogAsync("USER_REGISTERED", "User", user.Id,
                    $"New user registered: {user.Email}");

                // Sign the user in immediately after registration
                await _signInManager.SignInAsync(user, isPersistent: false);

                return RedirectToAction("Index", "Home");
            }

            // Add Identity errors to ModelState for display in the view
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ---------------------------------------------------------------
        //  LOGIN
        // ---------------------------------------------------------------

        //
        // FUNCTION    : Login (GET)
        // DESCRIPTION : Displays the login form.
        // PARAMETERS  : string? returnUrl : optional URL to redirect to after login
        // RETURNS     : IActionResult : the Login view
        //
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        //
        // FUNCTION    : Login (POST)
        // DESCRIPTION : Authenticates the user using ASP.NET Identity. Checks 
        //               for banned accounts. Logs both success and failure events.
        //               Rate-limited to 5 attempts per 15 minutes (OWASP A07).
        // PARAMETERS  : LoginViewModel model : the login form data
        //               string? returnUrl     : optional redirect URL
        // RETURNS     : Task<IActionResult> : redirects on success, 
        //               or returns the form with errors
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        [EnableRateLimiting("login-policy")]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Look up the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user != null)
            {
                // Check if the user has been banned by an admin
                if (user.IsBanned)
                {
                    await _auditService.LogAsync("LOGIN_BANNED", "User", user.Id,
                        $"Banned user attempted login: {model.Email}");

                    ModelState.AddModelError(string.Empty,
                        "Your account has been suspended. Contact an administrator.");
                    return View(model);
                }
            }

            // Attempt sign-in with lockout enabled (5 failed attempts = 15 min lockout)
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                await _auditService.LogAsync("LOGIN_SUCCESS", "User", user?.Id,
                    $"User logged in: {model.Email}");

                // Only redirect to returnUrl if it is local AND not a POST-only endpoint.
                // POST-only endpoints (ChangePassword, UploadAvatar, Logout) return 405
                // when reached via a browser GET after a login redirect.
                string[] postOnlyPaths = { "/Account/ChangePassword", "/Account/UploadAvatar", "/Account/Logout" };
                bool returnUrlIsPostOnly = !string.IsNullOrEmpty(returnUrl) &&
                    postOnlyPaths.Any(p => returnUrl.StartsWith(p, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) && !returnUrlIsPostOnly)
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync("LOGIN_LOCKED", "User", user?.Id,
                    $"Account locked due to repeated failures: {model.Email}");

                ModelState.AddModelError(string.Empty,
                    "Account locked due to too many failed attempts. Try again in 15 minutes.");
                return View(model);
            }

            // Log failed login attempt
            await _auditService.LogAsync("LOGIN_FAIL", "User", user?.Id,
                $"Failed login attempt for: {model.Email}");

            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(model);
        }

        // ---------------------------------------------------------------
        //  LOGOUT
        // ---------------------------------------------------------------

        //
        // FUNCTION    : Logout
        // DESCRIPTION : Signs the current user out and redirects to the home page.
        //               Logs the logout event.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : redirect to Home
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _auditService.LogAsync("LOGOUT", "User", null, "User logged out");
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ---------------------------------------------------------------
        //  PROFILE
        // ---------------------------------------------------------------

        //
        // FUNCTION    : Profile (GET)
        // DESCRIPTION : Displays the current user's profile page, including
        //               their avatar, account info, and all posts they created.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the Profile view
        //
        [HttpGet]
        [Authorize]
        [ResponseCache(NoStore = true, Duration = 0, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var myPosts = await _context.Posts
                .Where(p => p.AuthorId == user.Id && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Category)
                .ToListAsync();

            var viewModel = new ProfileViewModel
            {
                Email = user.Email ?? string.Empty,
                ProfilePicturePath = user.ProfilePicturePath,
                CreatedAt = user.CreatedAt,
                MyPosts = myPosts
            };

            return View(viewModel);
        }

        //
        // FUNCTION    : UploadAvatar (POST)
        // DESCRIPTION : Validates and saves a new profile picture for the current user.
        //               Only image types are accepted (JPEG, PNG, GIF, WEBP), max 2 MB.
        //               Stored with a GUID filename to prevent path traversal.
        // PARAMETERS  : IFormFile avatar : the uploaded image file
        // RETURNS     : Task<IActionResult> : redirects back to Profile
        //
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile avatar)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            if (avatar == null || avatar.Length == 0)
            {
                TempData["AvatarError"] = "No file selected.";
                return RedirectToAction("Profile");
            }

            if (avatar.Length > MaxAvatarSizeBytes)
            {
                TempData["AvatarError"] = "Image must be under 2 MB.";
                return RedirectToAction("Profile");
            }

            if (!AvatarContentTypes.Contains(avatar.ContentType.ToLower()))
            {
                TempData["AvatarError"] = "Only JPEG, PNG, GIF, and WEBP images are allowed.";
                return RedirectToAction("Profile");
            }

            var ext = Path.GetExtension(avatar.FileName).ToLower();
            if (!AvatarExtensions.Contains(ext))
            {
                TempData["AvatarError"] = "Invalid file extension.";
                return RedirectToAction("Profile");
            }

            var storedFileName = $"avatar_{Guid.NewGuid()}{ext}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            using (var stream = new FileStream(Path.Combine(uploadsFolder, storedFileName), FileMode.Create))
            {
                await avatar.CopyToAsync(stream);
            }

            user.ProfilePicturePath = storedFileName;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync("AVATAR_UPDATED", "User", user.Id,
                $"User {user.Email} updated their profile picture.");

            TempData["AvatarSuccess"] = "Profile picture updated.";
            return RedirectToAction("Profile");
        }

        //
        // FUNCTION    : ChangePassword (POST)
        // DESCRIPTION : Changes the current user's password after verifying the
        //               current password. Re-signs the user in so the session remains
        //               valid after the security stamp changes.
        // PARAMETERS  : ChangePasswordViewModel model : current + new password
        // RETURNS     : Task<IActionResult> : redirects back to Profile on success
        //
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["PasswordError"] = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Invalid input.";
                return RedirectToAction("Profile");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                // Re-sign in so the auth cookie reflects the new security stamp
                await _signInManager.RefreshSignInAsync(user);

                await _auditService.LogAsync("PASSWORD_CHANGED", "User", user.Id,
                    $"User {user.Email} changed their password.");

                TempData["PasswordSuccess"] = "Password changed successfully.";
            }
            else
            {
                TempData["PasswordError"] = result.Errors.FirstOrDefault()?.Description
                    ?? "Password change failed.";
            }

            return RedirectToAction("Profile");
        }
    }
}
