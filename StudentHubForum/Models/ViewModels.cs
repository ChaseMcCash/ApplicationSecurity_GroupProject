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

        // Optional file attachment (JPEG, PNG, GIF, WEBP, PDF, TXT — max 5 MB)
        [Display(Name = "Attachment (optional)")]
        public IFormFile? Attachment { get; set; }

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
    /// ViewModel for the user profile page.
    /// </summary>
    public class ProfileViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string? ProfilePicturePath { get; set; }
        public DateTime CreatedAt { get; set; }

        // Optional new avatar to upload
        [Display(Name = "Profile Picture")]
        public IFormFile? AvatarUpload { get; set; }

        // All posts created by this user (any status)
        public IEnumerable<Post> MyPosts { get; set; } = new List<Post>();
    }

    /// <summary>
    /// ViewModel for the change-password form on the profile page.
    /// </summary>
    public class ChangePasswordViewModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 10, ErrorMessage = "Password must be at least 10 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel for the home page — recent posts plus the logged-in user's post statuses.
    /// </summary>
    public class HomeViewModel
    {
        public IEnumerable<Post> RecentPosts { get; set; } = new List<Post>();

        // Posts by the current user that are still awaiting admin approval
        public IEnumerable<Post> MyPendingPosts { get; set; } = new List<Post>();

        // Most recent post by the current user that was approved
        public Post? MyLastApprovedPost { get; set; }
    }

    /// <summary>
    /// ViewModel for search results page with pagination support.
    /// </summary>
    public class SearchResultsViewModel
    {
        public string Query { get; set; } = string.Empty;
        public IEnumerable<Post> Results { get; set; } = new List<Post>();
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
