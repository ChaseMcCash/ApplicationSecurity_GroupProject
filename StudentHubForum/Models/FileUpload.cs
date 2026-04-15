// 
// FILE          : FileUpload.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the FileUpload entity class. It tracks all files 
//   uploaded to the system, including the original filename, a GUID-based 
//   stored filename (to prevent path traversal), content type, and size.
//   Files can optionally be associated with a specific post.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Represents a file uploaded by a user, optionally attached to a post.
    /// </summary>
    public class FileUpload
    {
        public int Id { get; set; }

        // Nullable FK — file may be a profile picture (no post) or a post attachment
        public int? PostId { get; set; }

        // Foreign key to AspNetUsers — who uploaded this file
        [Required]
        public string UploadedByUserId { get; set; } = string.Empty;

        // The original filename as provided by the user (for display only)
        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        // The GUID-based filename used for actual storage on disk
        [Required]
        [StringLength(260)]
        public string StoredFileName { get; set; } = string.Empty;

        // MIME content type (e.g., "image/jpeg", "application/pdf")
        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        // File size in bytes — used to enforce size limits
        public long FileSizeBytes { get; set; }

        // Timestamp of when the file was uploaded
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Post? Post { get; set; }
        public ApplicationUser? UploadedBy { get; set; }
    }
}
