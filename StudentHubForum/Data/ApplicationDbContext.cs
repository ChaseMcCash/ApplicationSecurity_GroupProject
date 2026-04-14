// 
// FILE          : ApplicationDbContext.cs
// PROJECT       : SECU2000 - Secure Client-Server Application
// PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
// FIRST VERSION : 2026-04-13
// DESCRIPTION   :
//   This file contains the Entity Framework Core DbContext for the 
//   StudentHub Forum application. It extends IdentityDbContext to 
//   include ASP.NET Identity tables alongside the application's custom 
//   tables (Posts, Comments, Categories, FileUploads, AuditLogs).
//   All database access in the application flows through this context,
//   ensuring strict separation between the UI and database layers.
// 

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentHubForum.Models;

namespace StudentHubForum.Data
{
    /// <summary>
    /// The primary database context for the StudentHub Forum application.
    /// Inherits from IdentityDbContext to integrate ASP.NET Identity.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        //
        // FUNCTION    : ApplicationDbContext (constructor)
        // DESCRIPTION : Passes configuration options to the base IdentityDbContext.
        // PARAMETERS  : DbContextOptions<ApplicationDbContext> options : EF Core config options
        // RETURNS     : N/A (constructor)
        //
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet properties — each corresponds to a database table
        public DbSet<Post> Posts { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<FileUpload> FileUploads { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        //
        // FUNCTION    : OnModelCreating
        // DESCRIPTION : Configures entity relationships and seeds initial category data.
        //               This method is called by EF Core during model creation.
        // PARAMETERS  : ModelBuilder modelBuilder : the EF Core model builder
        // RETURNS     : void
        //
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Post -> Author relationship (no cascade delete to avoid cycles)
            modelBuilder.Entity<Post>()
                .HasOne(p => p.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(p => p.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure Comment -> Post relationship (cascade delete)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(c => c.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure Comment -> Author relationship (no cascade to avoid cycles)
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Author)
                .WithMany(u => u.Comments)
                .HasForeignKey(c => c.AuthorId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure FileUpload -> Post (optional, no cascade)
            modelBuilder.Entity<FileUpload>()
                .HasOne(f => f.Post)
                .WithMany(p => p.Attachments)
                .HasForeignKey(f => f.PostId)
                .OnDelete(DeleteBehavior.NoAction);

            // Configure FileUpload -> User (no cascade to avoid cycles)
            modelBuilder.Entity<FileUpload>()
                .HasOne(f => f.UploadedBy)
                .WithMany()
                .HasForeignKey(f => f.UploadedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Seed initial categories
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "General Discussion", Description = "Open topics and general conversation" },
                new Category { Id = 2, Name = "Homework Help", Description = "Ask questions about assignments" },
                new Category { Id = 3, Name = "Announcements", Description = "Important updates from administrators" },
                new Category { Id = 4, Name = "Study Groups", Description = "Find and organize study sessions" }
            );
        }
    }
}
