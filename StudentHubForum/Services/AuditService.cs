// 
// FILE          : AuditService.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the IAuditService interface and its implementation.
//   The AuditService records all security-relevant actions to the AuditLogs 
//   database table, supporting OWASP A09 (Security Logging and Monitoring 
//   Failures). It captures the user identity, action type, affected entity, 
//   client IP address, and timestamp for every logged event.
// 

using System.Security.Claims;
using StudentHubForum.Data;
using StudentHubForum.Models;

namespace StudentHubForum.Services
{
    /// <summary>
    /// Interface for the audit logging service.
    /// </summary>
    public interface IAuditService
    {
        //
        // FUNCTION    : LogAsync
        // DESCRIPTION : Records a security-relevant action to the AuditLogs table.
        // PARAMETERS  : string action      : the action identifier (e.g., "LOGIN_SUCCESS")
        //               string? entityType  : the type of entity affected (e.g., "Post")
        //               string? entityId    : the ID of the affected entity
        //               string? details     : additional context about the action
        // RETURNS     : Task : asynchronous operation
        //
        Task LogAsync(
            string action,
            string? entityType = null,
            string? entityId = null,
            string? details = null);
    }

    /// <summary>
    /// Implementation of IAuditService that writes to the AuditLogs database table.
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        //
        // FUNCTION    : AuditService (constructor)
        // DESCRIPTION : Injects the database context and HTTP context accessor 
        //               to capture user identity and IP address.
        // PARAMETERS  : ApplicationDbContext context    : the EF Core database context
        //               IHttpContextAccessor httpAccessor : provides access to the HTTP request
        // RETURNS     : N/A (constructor)
        //
        public AuditService(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        //
        // FUNCTION    : LogAsync
        // DESCRIPTION : Creates an AuditLog entry with the current user's identity 
        //               and the client's IP address, then saves it to the database.
        // PARAMETERS  : string action      : the action identifier
        //               string? entityType  : optional entity type
        //               string? entityId    : optional entity ID
        //               string? details     : optional additional details
        // RETURNS     : Task : asynchronous operation
        //
        public async Task LogAsync(
            string action,
            string? entityType = null,
            string? entityId = null,
            string? details = null)
        {
            var httpContext = _httpContextAccessor.HttpContext;

            var auditLog = new AuditLog
            {
                UserId = httpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = httpContext?.User?.Identity?.Name,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown",
                Timestamp = DateTime.UtcNow,
                Details = details
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }
    }
}
