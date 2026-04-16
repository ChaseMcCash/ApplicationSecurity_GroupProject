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
        private const int PageSize = 5;

        //
        // FUNCTION    : Index
        // DESCRIPTION : Searches for posts matching the query string. Uses EF Core's
        //               LINQ provider which translates to parameterized SQL (safe from injection).
        //               When no query is provided, all approved posts are shown.
        //               Results are paginated at 5 posts per page.
        // PARAMETERS  : string query : the user's search text (optional)
        //               int page     : the current page number (1-based)
        // RETURNS     : Task<IActionResult> : the search results view
        //
        public async Task<IActionResult> Index(string query, int page = 1)
        {
            if (page < 1) page = 1;
            // Searches for posts matching the query string. Uses EF Core's
           //LINQ provider which translates to parameterized SQL 
            var baseQuery = _context.Posts
                .Where(p => !p.IsDeleted && p.IsApproved);

            // Filter by title when a query is provided; otherwise show all posts
            if (!string.IsNullOrWhiteSpace(query))
            {
                baseQuery = baseQuery.Where(p => p.Title.Contains(query));
            }

            var totalCount = await baseQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)PageSize);
            if (totalPages < 1) totalPages = 1;
            if (page > totalPages) page = totalPages;

            var results = await baseQuery
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Include(p => p.Author)
                .Include(p => p.Category)
                .ToListAsync();

            var viewModel = new SearchResultsViewModel
            {
                Query = query ?? string.Empty,
                Results = results,
                Page = page,
                TotalPages = totalPages
            };

            return View(viewModel);
        }
    }
}
