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

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models;
using StudentHubForum.Models.ViewModels;
using System.Security.Claims;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Serves the home page and error page.
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        //
        // FUNCTION    : HomeController (constructor)
        // DESCRIPTION : Injects the database context and user manager.
        // PARAMETERS  : ApplicationDbContext context                 : the EF Core database context
        //               UserManager<ApplicationUser> userManager     : retrieves the current user
        // RETURNS     : N/A (constructor)
        //
        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //
        // FUNCTION    : Index
        // DESCRIPTION : Displays the home page with the latest approved posts.
        //               When the user is logged in, also loads their pending posts
        //               and most recently approved post for the status widget.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the Index view with HomeViewModel
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

            var viewModel = new HomeViewModel { RecentPosts = recentPosts };

            // Load post status data only for authenticated users
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

                viewModel.MyPendingPosts = await _context.Posts
                    .Where(p => p.AuthorId == userId && !p.IsApproved && !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                viewModel.MyLastApprovedPost = await _context.Posts
                    .Where(p => p.AuthorId == userId && p.IsApproved && !p.IsDeleted)
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefaultAsync();
            }

            return View(viewModel);
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
