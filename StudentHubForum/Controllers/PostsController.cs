// 
// FILE          : PostsController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the PostsController for managing forum posts.
//   It handles creating new posts, viewing post details, and adding 
//   comments. All actions require authentication. Post content is 
//   rendered using Razor's default encoding to prevent XSS attacks.
//   All actions are logged via the AuditService.
// 

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models;
using StudentHubForum.Models.ViewModels;
using StudentHubForum.Services;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Manages forum post creation, viewing, and commenting.
    /// </summary>
    [Authorize]
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IWebHostEnvironment _environment;

        // Attachment security constants — mirrors UploadController whitelist
        private static readonly HashSet<string> AllowedContentTypes = new()
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "application/pdf", "text/plain"
        };
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".txt" };
        private const long MaxAttachmentBytes = 5 * 1024 * 1024; // 5 MB

        //
        // FUNCTION    : PostsController (constructor)
        // DESCRIPTION : Injects the database context, audit service, and hosting environment.
        // PARAMETERS  : ApplicationDbContext context       : the EF Core database context
        //               IAuditService auditService          : logs security events
        //               IWebHostEnvironment environment     : provides web root path for uploads
        // RETURNS     : N/A (constructor)
        //
        public PostsController(ApplicationDbContext context, IAuditService auditService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _auditService = auditService;
            _environment = environment;
        }

        //
        // FUNCTION    : Index
        // DESCRIPTION : Displays all approved, non-deleted posts ordered by creation date.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the post listing view
        //
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var posts = await _context.Posts
                .Where(p => p.IsApproved && !p.IsDeleted)
                .OrderByDescending(p => p.CreatedAt)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .ToListAsync();

            return View(posts);
        }

        //
        // FUNCTION    : Detail
        // DESCRIPTION : Displays a single post with its comments and attachments.
        //               Uses Razor's default encoding (@Model.Content) to prevent XSS.
        // PARAMETERS  : int id : the post ID
        // RETURNS     : Task<IActionResult> : the Detail view or NotFound
        //
        [AllowAnonymous]
        public async Task<IActionResult> Detail(int id)
        {
            var post = await _context.Posts
                .Where(p => p.Id == id && !p.IsDeleted)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .Include(p => p.Comments)
                    .ThenInclude(c => c.Author)
                .Include(p => p.Attachments)
                .FirstOrDefaultAsync();

            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        //
        // FUNCTION    : Create (GET)
        // DESCRIPTION : Displays the post creation form with a category dropdown.
        // PARAMETERS  : none
        // RETURNS     : Task<IActionResult> : the Create view with categories
        //
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new CreatePostViewModel
            {
                Categories = await _context.Categories.ToListAsync()
            };

            return View(viewModel);
        }

        //
        // FUNCTION    : Create (POST)
        // DESCRIPTION : Processes the post creation form. Validates input, 
        //               associates the post with the current user, and logs the event.
        // PARAMETERS  : CreatePostViewModel model : the form data
        // RETURNS     : Task<IActionResult> : redirects to Index on success
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePostViewModel model)
        {
            if (!ModelState.IsValid) //rejects posts with invalid/missing fields
            {
                model.Categories = await _context.Categories.ToListAsync();
                return View(model);
            }

            // Validate attachment before saving the post so we can reject early
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                if (model.Attachment.Length > MaxAttachmentBytes)
                {
                    ModelState.AddModelError("Attachment", "File must be under 5 MB.");
                    model.Categories = await _context.Categories.ToListAsync();
                    return View(model);
                }

                if (!AllowedContentTypes.Contains(model.Attachment.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Attachment", "Only JPEG, PNG, GIF, WEBP, PDF, and TXT files are allowed.");
                    model.Categories = await _context.Categories.ToListAsync();
                    return View(model);
                }

                var ext = Path.GetExtension(model.Attachment.FileName).ToLower();
                if (!AllowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("Attachment", "Invalid file extension.");
                    model.Categories = await _context.Categories.ToListAsync();
                    return View(model);
                }
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var post = new Post
            {
                Title = model.Title,
                Content = model.Content,
                AuthorId = userId,
                CategoryId = model.CategoryId,
                IsApproved = false  // new posts require admin approval before being visible
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            // Save attachment if one was provided and passed validation
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                var ext = Path.GetExtension(model.Attachment.FileName).ToLower();
                var storedFileName = $"post_{Guid.NewGuid()}{ext}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                Directory.CreateDirectory(uploadsFolder);

                using (var stream = new FileStream(Path.Combine(uploadsFolder, storedFileName), FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(stream);
                }

                var fileUpload = new FileUpload
                {
                    PostId = post.Id,
                    UploadedByUserId = userId,
                    OriginalFileName = Path.GetFileName(model.Attachment.FileName),
                    StoredFileName = storedFileName,
                    ContentType = model.Attachment.ContentType.ToLower(),
                    FileSizeBytes = model.Attachment.Length
                };

                _context.FileUploads.Add(fileUpload);
                await _context.SaveChangesAsync();

                await _auditService.LogAsync("FILE_UPLOADED", "FileUpload", fileUpload.Id.ToString(),
                    $"Attachment '{fileUpload.OriginalFileName}' uploaded with post {post.Id}");
            }

            // Log the post creation event
            await _auditService.LogAsync("POST_CREATED", "Post", post.Id.ToString(),
                $"Post '{post.Title}' created by user {userId}");

            return RedirectToAction(nameof(Index));
        }

        //
        // FUNCTION    : AddComment
        // DESCRIPTION : Adds a comment to a post. Validates that the content is 
        //               not empty and that the post exists. Logs the event.
        // PARAMETERS  : int postId      : the ID of the post to comment on
        //               string content  : the comment text
        // RETURNS     : Task<IActionResult> : redirects to the post detail page
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int postId, string content)
        {
            // Validate comment content, caps comment length, limiting payload size
            if (string.IsNullOrWhiteSpace(content) || content.Length > 1000)
            {
                return RedirectToAction(nameof(Detail), new { id = postId });
            }

            // Verify the post exists and is not deleted
            var post = await _context.Posts.FindAsync(postId);
            if (post == null || post.IsDeleted)
            {
                return NotFound();
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var comment = new Comment
            {
                PostId = postId,
                AuthorId = userId,
                Content = content
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Log the comment event
            await _auditService.LogAsync("COMMENT_ADDED", "Comment", comment.Id.ToString(),
                $"Comment added to post {postId}");

            return RedirectToAction(nameof(Detail), new { id = postId });
        }
    }
}
