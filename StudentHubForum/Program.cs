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
