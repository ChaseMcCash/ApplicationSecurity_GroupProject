// 
// FILE          : Category.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the Category entity class. Categories are used 
//   to organize forum posts into logical groups (e.g., General, Homework, 
//   Announcements). Each category can contain many posts.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Represents a forum category for organizing posts.
    /// </summary>
    public class Category
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation property: all posts in this category
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
