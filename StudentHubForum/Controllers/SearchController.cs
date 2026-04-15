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

            // this is the vulnerable version of search (for demo purposes)
            // instead of using safe EF queries, we directly inject the user input into SQL
            // this is BAD practice and allows SQL Injection attacks

            if (!string.IsNullOrWhiteSpace(query))
            {
                // this is the vulnerable version of the search feature for demo purposes
                // instead of using safe queries, we are directly inserting user input into SQL
                // this is bad practice because it allows SQL Injection attacks

                viewModel.Results = await _context.Posts

                    // here we are building a raw SQL query using string interpolation
                    // whatever the user types in the search bar gets placed directly into the SQL
                    // this is what makes it vulnerable to things like: ' OR 1=1 --
                    .FromSqlRaw($"SELECT * FROM Posts WHERE Title LIKE '%{query}%'")

                    // we removed the IsApproved filter so we can clearly see the injected results
                    // otherwise it was hiding posts and making testing confusing
                    .OrderByDescending(p => p.CreatedAt)

                    // include related data so posts display properly (author + category)
                    .Include(p => p.Author)
                    .Include(p => p.Category)

                    // limit results so it doesn't return too much data
                    .Take(50)

                    .ToListAsync();
            }

            return View(viewModel);
        }
    }
}
