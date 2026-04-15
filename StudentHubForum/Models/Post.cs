// 
// FILE          : Post.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the Post entity class representing a forum thread.
//   Each post belongs to a category, has an author, and can contain 
//   multiple comments and file attachments. Posts support soft-delete 
//   and admin approval workflows.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Represents a forum post (thread) in the StudentHub application.
    /// </summary>
    public class Post
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        // Foreign key to AspNetUsers — the post's author
        [Required]
        public string AuthorId { get; set; } = string.Empty;

        // Foreign key to Categories
        [Required]
        public int CategoryId { get; set; }

        // Timestamp of when the post was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Admin must approve posts before they are publicly visible
        public bool IsApproved { get; set; } = false;

        // Soft-delete flag — posts are never hard-deleted
        public bool IsDeleted { get; set; } = false;

        // Navigation properties
        public ApplicationUser? Author { get; set; }
        public Category? Category { get; set; }
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<FileUpload> Attachments { get; set; } = new List<FileUpload>();
    }
}
