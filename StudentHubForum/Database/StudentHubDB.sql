-- 
-- FILE          : StudentHubDB.sql
-- PROJECT       : SECU2000 - Secure Client-Server Application
-- PROGRAMMER    : Burhan Shibli, Tuan Thanh Nguyen, Chase Mccash
-- FIRST VERSION : 2026-04-05
-- DESCRIPTION   :
--   StudentHubDB.sql defines the database schema for the StudentHub application, 
--   which is a secure client-server application designed for students to share posts, comments, 
--   and files. The database includes tables for user management (via ASP.NET Identity), 
--   content management (posts, comments, categories), file uploads, and audit logging for security monitoring.
--    

-- Created automatically by ASP.NET Identity:
-- AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, etc.

-- Extended columns on AspNetUsers (added via migration):
-- ProfilePicturePath NVARCHAR(500)
-- CreatedAt DATETIME2 DEFAULT GETDATE()
-- IsBanned BIT DEFAULT 0

CREATE TABLE Categories (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL
);

CREATE TABLE Posts (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(200) NOT NULL,
    Content     NVARCHAR(MAX) NOT NULL,
    AuthorId    NVARCHAR(450) NOT NULL,        -- FK to AspNetUsers.Id
    CategoryId  INT NOT NULL,                  -- FK to Categories.Id
    CreatedAt   DATETIME2 DEFAULT GETDATE(),
    IsApproved  BIT DEFAULT 0,
    IsDeleted   BIT DEFAULT 0,
    FOREIGN KEY (AuthorId)   REFERENCES AspNetUsers(Id),
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
);

CREATE TABLE Comments (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    PostId    INT NOT NULL,
    AuthorId  NVARCHAR(450) NOT NULL,
    Content   NVARCHAR(1000) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (PostId)    REFERENCES Posts(Id) ON DELETE CASCADE,
    FOREIGN KEY (AuthorId)  REFERENCES AspNetUsers(Id)
);

CREATE TABLE FileUploads (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    PostId          INT NULL,
    UploadedByUserId NVARCHAR(450) NOT NULL,
    OriginalFileName NVARCHAR(260) NOT NULL,
    StoredFileName  NVARCHAR(260) NOT NULL,    -- GUID-based safe name
    ContentType     NVARCHAR(100) NOT NULL,
    FileSizeBytes   BIGINT NOT NULL,
    UploadedAt      DATETIME2 DEFAULT GETDATE(),
    FOREIGN KEY (PostId)           REFERENCES Posts(Id),
    FOREIGN KEY (UploadedByUserId) REFERENCES AspNetUsers(Id)
);

CREATE TABLE AuditLogs (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    UserId     NVARCHAR(450) NULL,             -- NULL for anonymous actions
    UserName   NVARCHAR(256) NULL,
    Action     NVARCHAR(100) NOT NULL,         -- e.g. "LOGIN_SUCCESS", "POST_DELETED"
    EntityType NVARCHAR(50)  NULL,             -- e.g. "Post", "User"
    EntityId   NVARCHAR(50)  NULL,
    IpAddress  NVARCHAR(45)  NOT NULL,
    Timestamp  DATETIME2 DEFAULT GETDATE(),
    Details    NVARCHAR(MAX) NULL
);