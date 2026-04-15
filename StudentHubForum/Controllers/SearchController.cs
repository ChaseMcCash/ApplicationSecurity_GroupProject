// 
// FILE          : SearchController.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the SearchController for the forum's search 
//   functionality. It uses EF Core's LINQ-to-SQL translation which 
//   automatically generates parameterized queries, preventing SQL 
//   Injection attacks (OWASP A03). Search results are scoped to only 
//   approved, non-deleted posts with a result limit to prevent data 
//   dumping.
// 

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Data;
using StudentHubForum.Models.ViewModels;

namespace StudentHubForum.Controllers
{
    /// <summary>
    /// Handles search functionality with parameterized queries (OWASP A03 mitigation).
    /// </summary>
    public class SearchController : Controller
    {
        private readonly ApplicationDbContext _context;

        //
        // FUNCTION    : SearchController (constructor)
        // DESCRIPTION : Injects the database context.
        // PARAMETERS  : ApplicationDbContext context : the EF Core database context
        // RETURNS     : N/A (constructor)
        //
        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        //
        // FUNCTION    : Index
        // DESCRIPTION : Searches for posts matching the query string. Uses EF Core's 
        //               LINQ provider which translates to parameterized SQL:
        //               WHERE [p].[Title] LIKE @__query_0 (safe from injection).
        //               Results are limited to 50 to prevent data dumping.
        // PARAMETERS  : string query : the user's search text
        // RETURNS     : Task<IActionResult> : the search results view
        //
        public async Task<IActionResult> Index(string query)
        {
            var viewModel = new SearchResultsViewModel { Query = query ?? string.Empty };

            // Only execute search if a query was provided
            if (!string.IsNullOrWhiteSpace(query))
            {
                // EF Core translates this to parameterized SQL:
                //   SELECT ... FROM Posts WHERE Title LIKE @p0 AND IsDeleted = 0 AND IsApproved = 1
                // The user's input is NEVER concatenated into the SQL string.
                viewModel.Results = await _context.Posts
                    .Where(p => p.Title.Contains(query) && !p.IsDeleted && p.IsApproved)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(50) // Limit results to prevent data dumping
                    .Include(p => p.Author)
                    .Include(p => p.Category)
                    .ToListAsync();
            }

            return View(viewModel);
        }
    }
}
