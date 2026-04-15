// 
// FILE          : AdminController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the AdminController for the admin dashboard.
//   All actions are protected by [Authorize(Roles = "Admin")] which 
//   enforces role-based access control (OWASP A01). The admin can 
//   approve/delete posts, ban users, and view audit logs. All admin 
//   actions are logged for accountability (OWASP A09).
// 

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models.ViewModels;
using StudentHubForum.Services;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Admin dashboard — restricted to users with the "Admin" role (OWASP A01 mitigation).
    /// </summary>
    //[Authorize(Roles = "Admin")]//commenting it out to make the admin panel accessible without the admin role which would be a broken access control vulnerability
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        //
        // FUNCTION    : AdminController (constructor)
        // DESCRIPTION : Injects the database context and audit service.
        // PARAMETERS  : ApplicationDbContext context : the EF Core database context
        //               IAuditService auditService    : logs security events
        // RETURNS     : N/A (constructor)
        //
        public AdminController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        //
        // FUNCTION    : Index
        // DESCRIPTION : Displays the admin dashboard with all users, pending posts, 
        //               and the 50 most recent audit log entries.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the admin dashboard view
        //
        public async Task<IActionResult> Index()
        {
            // Log admin panel access
            await _auditService.LogAsync("ADMIN_ACCESS", "AdminPanel", null, "Admin dashboard accessed");

            var viewModel = new AdminDashboardViewModel
            {
                Users = await _context.Users.ToListAsync(),
                PendingPosts = await _context.Posts
                    .Where(p => !p.IsApproved && !p.IsDeleted)
                    .Include(p => p.Author)
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync(),
                RecentAuditLogs = await _context.AuditLogs
                    .OrderByDescending(l => l.Timestamp)
                    .Take(50)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        //
        // FUNCTION    : ApprovePost
        // DESCRIPTION : Approves a pending post, making it visible to all users.
        //               Logs the approval action.
        // PARAMETERS  : int id : the post ID to approve
        // RETURNS     : Task<IActionResult> : redirect to admin dashboard
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            post.IsApproved = true;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("POST_APPROVED", "Post", id.ToString(),
                $"Post '{post.Title}' approved by admin");

            return RedirectToAction(nameof(Index));
        }

        //
        // FUNCTION    : DeletePost
        // DESCRIPTION : Soft-deletes a post (sets IsDeleted = true). Posts are 
        //               never hard-deleted to preserve audit history.
        // PARAMETERS  : int id : the post ID to delete
        // RETURNS     : Task<IActionResult> : redirect to admin dashboard
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            // Soft delete — never remove data permanently
            post.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("POST_DELETED", "Post", id.ToString(),
                $"Post '{post.Title}' soft-deleted by admin");

            return RedirectToAction(nameof(Index));
        }

        //
        // FUNCTION    : BanUser
        // DESCRIPTION : Bans a user account, preventing them from logging in.
        //               Logs the ban action.
        // PARAMETERS  : string userId : the user ID to ban
        // RETURNS     : Task<IActionResult> : redirect to admin dashboard
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BanUser(string userId)
        {
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.IsBanned = true;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("USER_BANNED", "User", userId,
                $"User '{user.UserName}' banned by admin");

            return RedirectToAction(nameof(Index));
        }
    }
}
