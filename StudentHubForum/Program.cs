// 
// FILE          : Program.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This is the main entry point for the StudentHub Forum application.
//   It configures all services, middleware, and security controls:
//   - ASP.NET Identity with password policy and account lockout (A07)
//   - Rate limiting on login and API endpoints (A07)
//   - CSRF protection via AutoValidateAntiforgeryToken (CSRF)
//   - Security headers: CSP, X-Frame-Options, X-Content-Type-Options (A05)
//   - HTTPS redirection and HSTS (A05)
//   - Environment-aware error handling — no stack traces in production (A05)
//   - Role and admin user seeding on startup
// 

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models;
using StudentHubForum.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
//  DATABASE — EF Core with SQL Server
// ---------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ---------------------------------------------------------------
//  IDENTITY — Password policy + Account lockout (OWASP A07)
// ---------------------------------------------------------------
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Strong password requirements
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Account lockout after 5 failed attempts for 15 minutes
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Email confirmation disabled for demo (noted in report)
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings for session security
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;       // Prevent JavaScript access to cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Login";
});

// ---------------------------------------------------------------
//  RATE LIMITING (OWASP A07)
// ---------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    // Login endpoint: max 5 attempts per 15 minutes per IP
    options.AddFixedWindowLimiter("login-policy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(15);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // API endpoints: max 100 requests per minute
    options.AddFixedWindowLimiter("api-policy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 100;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    options.RejectionStatusCode = 429; // HTTP 429 Too Many Requests
});

// ---------------------------------------------------------------
//  MVC + CSRF Protection
// ---------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // Automatically validate anti-forgery tokens on all POST actions
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

// ---------------------------------------------------------------
//  SERVICES
// ---------------------------------------------------------------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditService, AuditService>();

var app = builder.Build();

// ---------------------------------------------------------------
//  ERROR HANDLING (OWASP A05)
// ---------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    // Developer exception page — only in Development environment
    app.UseDeveloperExceptionPage();
}
else
{
    // Generic error page — hides stack traces, SQL errors, file paths
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // HTTP Strict Transport Security
}

// ---------------------------------------------------------------
//  SECURITY HEADERS MIDDLEWARE (OWASP A05)
// ---------------------------------------------------------------
app.Use(async (context, next) =>
{
    // Content-Security-Policy: only allow resources from same origin
    context.Response.Headers.Append("Content-Security-Policy",
    "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
    "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net;");

    // Prevent MIME type sniffing
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

    // Prevent clickjacking
    context.Response.Headers.Append("X-Frame-Options", "DENY");

    // Referrer policy
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ---------------------------------------------------------------
//  ROUTE MAPPING
// ---------------------------------------------------------------
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapControllers(); // Map API controllers

// ---------------------------------------------------------------
//  SEED ROLES AND ADMIN USER
// ---------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // Create roles if they do not exist
    string[] roleNames = { "Admin", "Student" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed the admin account
    var adminEmail = "admin@studenthub.local";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, "Admin@12345!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }

    // Seed a test student account
    var studentEmail = "student@studenthub.local";
    if (await userManager.FindByEmailAsync(studentEmail) == null)
    {
        var studentUser = new ApplicationUser
        {
            UserName = studentEmail,
            Email = studentEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(studentUser, "Student@1234!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(studentUser, "Student");
        }
    }
}

app.Run();