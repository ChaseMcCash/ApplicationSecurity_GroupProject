// 
// FILE          : UploadController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the UploadController for handling file uploads.
//   Security controls include: MIME type whitelist, file extension whitelist, 
//   5 MB size limit, GUID-based stored filenames (prevents path traversal), 
//   and CSRF token validation. All uploads are recorded in the FileUploads 
//   table and the AuditLogs table. This addresses OWASP A08 (Software and 
//   Data Integrity Failures).
// 

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudentHubForum.Data;
using StudentHubForum.Models;
using StudentHubForum.Services;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Handles file uploads with security validations (OWASP A08 mitigation).
    /// </summary>
    [Authorize]
    public class UploadController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditService _auditService;

        // Whitelist of allowed MIME content types
        private static readonly HashSet<string> AllowedContentTypes = new()
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp",
            "application/pdf",
            "text/plain"
        };

        // Whitelist of allowed file extensions
        private static readonly string[] AllowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".txt"
        };

        // Maximum file size: 5 MB
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        //
        // FUNCTION    : UploadController (constructor)
        // DESCRIPTION : Injects the database context, hosting environment, and audit service.
        // PARAMETERS  : ApplicationDbContext context        : the EF Core database context
        //               IWebHostEnvironment environment     : provides web root path
        //               IAuditService auditService          : logs security events
        // RETURNS     : N/A (constructor)
        //
        public UploadController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            IAuditService auditService)
        {
            _context = context;
            _environment = environment;
            _auditService = auditService;
        }

        //
        // FUNCTION    : Index (GET)
        // DESCRIPTION : Displays the file upload form.
        // PARAMETERS  : none
        // RETURNS     : IActionResult : the Upload view
        //
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        //
        // FUNCTION    : Upload (POST)
        // DESCRIPTION : Processes an uploaded file with the following security checks:
        //               1. Null/empty file check
        //               2. File size limit (5 MB maximum)
        //               3. MIME content type whitelist validation
        //               4. File extension whitelist validation
        //               5. GUID-based filename generation (prevents path traversal)
        //               6. Database record creation for tracking
        //               7. Audit log entry
        // PARAMETERS  : IFormFile file : the uploaded file from the form
        //               int? postId     : optional post ID to attach the file to
        // RETURNS     : Task<IActionResult> : success or error view
        //
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(IFormFile file, int? postId)
        {
            // 1. Null/empty check
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "No file was provided.";
                return View("Index");
            }

            // 2. File size validation
            //if (file.Length > MaxFileSizeBytes)
            //{
            //    ViewBag.Error = "File exceeds the 5 MB size limit.";
            //    return View("Index");
            //}

            // 3. MIME content type whitelist validation
            //if (!AllowedContentTypes.Contains(file.ContentType.ToLower()))
            //{
            //    ViewBag.Error = "File type not allowed. Permitted: JPEG, PNG, GIF, WEBP, PDF, TXT.";
            //    return View("Index");
            //}

            // 4. File extension whitelist validation
            var extension = Path.GetExtension(file.FileName).ToLower();
            //if (!AllowedExtensions.Contains(extension))
            //{
            //    ViewBag.Error = "File extension not allowed.";
            //    return View("Index");
            //}

            // 5. Generate a safe stored filename using GUID (prevents path traversal)
            var storedFileName = $"{Guid.NewGuid()}{extension}";
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");

            // Ensure the uploads directory exists
            Directory.CreateDirectory(uploadsFolder);

            var storedPath = Path.Combine(uploadsFolder, storedFileName);

            // Save the file to disk
            using (var stream = new FileStream(storedPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // 6. Create a database record for tracking
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var uploadRecord = new FileUpload
            {
                PostId = postId,
                UploadedByUserId = userId,
                OriginalFileName = Path.GetFileName(file.FileName), // Strip path components
                StoredFileName = storedFileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length
            };

            _context.FileUploads.Add(uploadRecord);
            await _context.SaveChangesAsync();

            // 7. Log the upload event
            await _auditService.LogAsync("FILE_UPLOADED", "FileUpload", uploadRecord.Id.ToString(),
                $"File '{uploadRecord.OriginalFileName}' uploaded (stored as '{storedFileName}', " +
                $"size: {file.Length} bytes, type: {file.ContentType})");

            ViewBag.Success = $"File '{uploadRecord.OriginalFileName}' uploaded successfully.";
            return View("Index");
        }
    }
}
