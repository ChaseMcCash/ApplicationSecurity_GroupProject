// 
// FILE          : ApplicationUser.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the ApplicationUser entity class which extends 
//   ASP.NET Identity's IdentityUser. It adds custom properties such as 
//   ProfilePicturePath, CreatedAt, and IsBanned to support the forum 
//   application's user management and security requirements.
// 

using Microsoft.AspNetCore.Identity;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Extends the built-in IdentityUser with application-specific properties
    /// for the StudentHub Forum Portal.
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        // Path to the user's uploaded profile picture (stored as GUID-based filename)
        public string? ProfilePicturePath { get; set; }

        // Timestamp of when the account was created
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Flag indicating whether an admin has banned this user
        public bool IsBanned { get; set; } = false;

        // Navigation property: all posts authored by this user
        public ICollection<Post> Posts { get; set; } = new List<Post>();

        // Navigation property: all comments authored by this user
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    }
}
