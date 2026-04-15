// 
// FILE          : ViewModels.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains all ViewModel classes used to transfer data 
//   between controllers and Razor views. ViewModels ensure that only 
//   the necessary data is exposed to the UI layer, following the 
//   principle of least privilege for data exposure.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models.ViewModels
{
    /// <summary>
    /// ViewModel for the user registration form.
    /// </summary>
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the user login form.
    /// </summary>
    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    /// <summary>
    /// ViewModel for creating a new forum post.
    /// </summary>
    public class CreatePostViewModel
    {
        [Required]
        [StringLength(200, MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Content { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // Available categories for the dropdown
        public IEnumerable<Category>? Categories { get; set; }
    }

    /// <summary>
    /// ViewModel for the admin dashboard displaying users, pending posts, and logs.
    /// </summary>
    public class AdminDashboardViewModel
    {
        public IEnumerable<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
        public IEnumerable<Post> PendingPosts { get; set; } = new List<Post>();
        public IEnumerable<AuditLog> RecentAuditLogs { get; set; } = new List<AuditLog>();
    }

    /// <summary>
    /// ViewModel for search results page.
    /// </summary>
    public class SearchResultsViewModel
    {
        public string Query { get; set; } = string.Empty;
        public IEnumerable<Post> Results { get; set; } = new List<Post>();
    }
}
