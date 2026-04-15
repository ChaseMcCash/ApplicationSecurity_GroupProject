// 
// FILE          : AuditLog.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the AuditLog entity class. It records all 
//   security-relevant actions in the system (logins, post deletions, 
//   file uploads, admin actions, etc.) to support OWASP A09 — Security 
//   Logging and Monitoring. Each entry captures the user, action type, 
//   IP address, and timestamp.
// 

using System.ComponentModel.DataAnnotations;

namespace StudentHubForum.Models
{
    /// <summary>
    /// Represents an audit log entry for security monitoring (OWASP A09).
    /// </summary>
    public class AuditLog
    {
        public int Id { get; set; }

        // The user who performed the action (NULL for anonymous actions)
        public string? UserId { get; set; }

        // Username at the time of the action (denormalized for historical accuracy)
        public string? UserName { get; set; }

        // Action identifier (e.g., "LOGIN_SUCCESS", "POST_DELETED", "FILE_UPLOADED")
        [Required]
        [StringLength(100)]
        public string Action { get; set; } = string.Empty;

        // The type of entity affected (e.g., "Post", "User", "FileUpload")
        [StringLength(50)]
        public string? EntityType { get; set; }

        // The ID of the affected entity
        [StringLength(50)]
        public string? EntityId { get; set; }

        // The IP address of the client that triggered the action
        [Required]
        [StringLength(45)]
        public string IpAddress { get; set; } = string.Empty;

        // Timestamp of when the action occurred
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Additional details or context about the action
        public string? Details { get; set; }
    }
}
