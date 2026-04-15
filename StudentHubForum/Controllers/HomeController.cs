// 
// FILE          : HomeController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the HomeController which serves the application's 
//   landing page and the generic error page. The error page intentionally 
//   hides internal details (stack traces, SQL errors) to prevent 
//   information disclosure (OWASP A05).
// 

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Serves the home page and error page.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        //
        // FUNCTION    : HomeController (constructor)
        // DESCRIPTION : Injects the database context.
        // PARAMETERS  : ApplicationDbContext context : the EF Core database context
        // RETURNS     : N/A (constructor)
        //
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        //
        // FUNCTION    : Index
        // DESCRIPTION : Displays the home page with the latest approved, non-deleted posts.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the Index view with recent posts
        //
        public async Task<IActionResult> Index()
        {
            var recentPosts = await _context.Posts
                .Where(p => p.IsApproved && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Take(10)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .ToListAsync();

            return View(recentPosts);
        }

        //
        // FUNCTION    : Error
        // DESCRIPTION : Displays a generic error page without exposing internal details.
        //               This prevents information disclosure (OWASP A05).
        // PARAMETERS  : none
        // RETURNS     : IActionResult : the Error view
        //
        public IActionResult Error()
        {
            return View();
        }
    }
}
