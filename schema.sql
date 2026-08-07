-- ============================================================
-- Workplace Knowledge Vault — Reusable Database Schema
-- ============================================================

-- 1. Create and Use Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'KnowledgeVaultDb')
BEGIN
    CREATE DATABASE KnowledgeVaultDb;
END
GO

USE KnowledgeVaultDb;
GO

-- Drop all tables if they exist (child tables first)
DROP TABLE IF EXISTS ArticleTags;
DROP TABLE IF EXISTS Tags;
DROP TABLE IF EXISTS Bookmarks;
DROP TABLE IF EXISTS Likes;
DROP TABLE IF EXISTS Comments;
DROP TABLE IF EXISTS Articles;
DROP TABLE IF EXISTS Categories;
DROP TABLE IF EXISTS Users;
GO

-- 2. Create Tables

CREATE TABLE Users (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    Email NVARCHAR(100) NOT NULL,
    PasswordHash NVARCHAR(255) NOT NULL,
    Role NVARCHAR(20) NOT NULL,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Categories (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL,
    Description NVARCHAR(255)
);

CREATE TABLE Articles (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Status NVARCHAR(20) NOT NULL,
    AuthorId INT FOREIGN KEY REFERENCES Users(Id),
    CategoryId INT FOREIGN KEY REFERENCES Categories(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL
);

CREATE TABLE Comments (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Content NVARCHAR(MAX) NOT NULL,
    ArticleId INT FOREIGN KEY REFERENCES Articles(Id),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Likes (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ArticleId INT FOREIGN KEY REFERENCES Articles(Id),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Bookmarks (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    ArticleId INT FOREIGN KEY REFERENCES Articles(Id),
    UserId INT FOREIGN KEY REFERENCES Users(Id),
    CreatedAt DATETIME2 DEFAULT GETUTCDATE()
);

CREATE TABLE Tags (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL
);

CREATE TABLE ArticleTags (
    ArticleId INT FOREIGN KEY REFERENCES Articles(Id) ON DELETE CASCADE,
    TagId INT FOREIGN KEY REFERENCES Tags(Id) ON DELETE CASCADE,
    PRIMARY KEY (ArticleId, TagId)
);
GO

-- 3. Insert Demo Data

INSERT INTO Users (Username, Email, PasswordHash, Role) VALUES
('admin', 'admin@vault.com', '$2a$11$XyQq2vJ1W8hXTa3sQn6zFOCVXs/2Oa3dBqJMbEFCYwD5UpkFhXfqS', 'Admin'),
('employee', 'employee@vault.com', '$2a$11$A9p5YZ3QdKj2Xm8Rn7wNOOzs1BtP0EYqD6VnL4Hk9c2IuM3rS5ae', 'Employee');

INSERT INTO Categories (Name, Description) VALUES
('Azure', 'Microsoft Cloud Services'),
('React', 'Frontend Web Framework'),
('SQL', 'Database Resources'),
('HR', 'Company Policies');

INSERT INTO Articles (Title, Content, Status, AuthorId, CategoryId) VALUES
('Getting Started with Azure', 'Azure App Service is a fully managed platform for building web apps.', 'Approved', 1, 1),
('React Basics Guide', 'React Hooks let you write stateful functional components cleanly.', 'Approved', 2, 2);
