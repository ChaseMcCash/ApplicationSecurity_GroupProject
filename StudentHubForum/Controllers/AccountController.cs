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

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using StudentHubForum.Models;
using StudentHubForum.Models.ViewModels;
using StudentHubForum.Services;

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
        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _auditService = auditService;
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

                // Redirect to the return URL if valid, otherwise go to Home
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
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
    }
}
