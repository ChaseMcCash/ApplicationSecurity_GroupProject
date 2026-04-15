// 
// FILE          : PostsApiController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the REST API controller for posts. It provides 
//   JSON endpoints for retrieving and deleting posts. All endpoints 
//   require authentication, and delete operations are restricted to 
//   Admin users only (OWASP A01). Rate limiting is applied to prevent 
//   abuse (OWASP A07).
// 

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Services;

namespace StudentHubForum.Controllers.Api
{
    /// <summary>
    /// REST API for posts — provides JSON endpoints for external consumers.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PostsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        //
        // FUNCTION    : PostsApiController (constructor)
        // DESCRIPTION : Injects the database context and audit service.
        // PARAMETERS  : ApplicationDbContext context : the EF Core database context
        //               IAuditService auditService    : logs security events
        // RETURNS     : N/A (constructor)
        //
        public PostsApiController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        //
        // FUNCTION    : GetAll
        // DESCRIPTION : Returns all approved, non-deleted posts as JSON.
        //               Rate-limited to 100 requests per minute per IP.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : JSON array of posts
        //
        [HttpGet]
        [EnableRateLimiting("api-policy")]
        public async Task<IActionResult> GetAll()
        {
            var posts = await _context.Posts
                .Where(p => p.IsApproved && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    Author = p.Author!.UserName,
                    Category = p.Category!.Name,
                    p.CreatedAt
                })
                .ToListAsync();

            return Ok(posts);
        }

        //
        // FUNCTION    : GetById
        // DESCRIPTION : Returns a single post by ID as JSON.
        // PARAMETERS  : int id : the post ID
        // RETURNS     : Task<IActionResult> : JSON object or 404 NotFound
        //
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var post = await _context.Posts
                .Where(p => p.Id == id && p.IsApproved && !p.IsDeleted)
                .Select(p => new
                {
                    p.Id,
                    p.Title,
                    p.Content,
                    Author = p.Author!.UserName,
                    Category = p.Category!.Name,
                    p.CreatedAt,
                    CommentCount = p.Comments.Count
                })
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound();
            }

            return Ok(post);
        }

        //
        // FUNCTION    : Delete
        // DESCRIPTION : Soft-deletes a post via the API. Restricted to Admin role only.
        //               Logs the deletion event.
        // PARAMETERS  : int id : the post ID to delete
        // RETURNS     : Task<IActionResult> : 204 NoContent or 404 NotFound
        //
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                return NotFound();
            }

            post.IsDeleted = true;
            await _context.SaveChangesAsync();

            await _auditService.LogAsync("API_POST_DELETED", "Post", id.ToString(),
                $"Post '{post.Title}' deleted via API");

            return NoContent();
        }
    }
}
