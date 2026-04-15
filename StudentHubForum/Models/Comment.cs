// 
// FILE          : Comment.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the Comment entity class. Comments belong to a 
//   specific Post and are authored by a registered user. They support 
//   cascade delete when a parent post is removed.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Represents a comment on a forum post.
    /// </summary>
    public class Comment
    {
        public int Id { get; set; }

        // Foreign key to Posts
        [Required]
        public int PostId { get; set; }

        // Foreign key to AspNetUsers — the comment's author
        [Required]
        public string AuthorId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        // Timestamp of when the comment was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Post? Post { get; set; }
        public ApplicationUser? Author { get; set; }
    }
}
