using KnowledgeVault.API.Models;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.DTOs
{
    // ─── Auth ───────────────────────────────────────────────
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    // ─── User DTO with Inline SQL ────────────────────────────
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public static async Task<List<UserDto>> FetchAllAsync(SqlConnection conn)
        {
            await conn.OpenAsync();
            var sql = "SELECT Id, Username, Email, Role, CreatedAt FROM Users ORDER BY CreatedAt DESC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<UserDto>();
            while (await reader.ReadAsync())
            {
                list.Add(new UserDto
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    Role = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }
            return list;
        }
    }

    // ─── Article Requests & DTOs with Inline SQL ─────────────
    public class CreateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class UpdateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class ApproveRequest
    {
        public string Status { get; set; } = string.Empty; // Approved | Rejected
    }

    public class ArticleListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public static async Task<List<ArticleListDto>> FetchByStatusAsync(SqlConnection conn, string status)
        {
            await conn.OpenAsync();
            var sql = @"
                SELECT a.Id, a.Title, a.Content, a.Status, a.AuthorId, u.Username AS AuthorName, 
                       a.CategoryId, c.Name AS CategoryName, a.CreatedAt,
                       (SELECT COUNT(*) FROM Likes l WHERE l.ArticleId = a.Id) AS LikeCount
                FROM Articles a
                INNER JOIN Users u ON a.AuthorId = u.Id
                LEFT JOIN Categories c ON a.CategoryId = c.Id
                WHERE a.Status = @Status
                ORDER BY a.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<ArticleListDto>();
            while (await reader.ReadAsync())
            {
                var content = reader.GetString(2);
                var dto = new ArticleListDto
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Excerpt = content.Length > 130 ? content.Substring(0, 130) + "..." : content,
                    Status = reader.GetString(3),
                    AuthorId = reader.GetInt32(4),
                    AuthorName = reader.GetString(5),
                    CategoryId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    CategoryName = reader.IsDBNull(7) ? null : reader.GetString(7),
                    CreatedAt = reader.GetDateTime(8),
                    LikeCount = reader.GetInt32(9),
                    Tags = new List<string>()
                };
                list.Add(dto);
            }
            reader.Close();

            foreach (var item in list)
            {
                item.Tags = await Tag.GetTagsForArticleAsync(conn, item.Id);
            }

            return list;
        }
    }

    public class ArticleDetailDto : ArticleListDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsLikedByUser { get; set; }
        public bool IsBookmarkedByUser { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
        public DateTime? UpdatedAt { get; set; }

        public static async Task<ArticleDetailDto?> FetchByIdAsync(SqlConnection conn, int id, int currentUserId)
        {
            await conn.OpenAsync();
            var sql = @"
                SELECT a.Id, a.Title, a.Content, a.Status, a.AuthorId, u.Username AS AuthorName, 
                       a.CategoryId, c.Name AS CategoryName, a.CreatedAt, a.UpdatedAt,
                       (SELECT COUNT(*) FROM Likes l WHERE l.ArticleId = a.Id) AS LikeCount,
                       (SELECT COUNT(*) FROM Likes l WHERE l.ArticleId = a.Id AND l.UserId = @UserId) AS UserLiked,
                       (SELECT COUNT(*) FROM Bookmarks b WHERE b.ArticleId = a.Id AND b.UserId = @UserId) AS UserBookmarked
                FROM Articles a
                INNER JOIN Users u ON a.AuthorId = u.Id
                LEFT JOIN Categories c ON a.CategoryId = c.Id
                WHERE a.Id = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            cmd.Parameters.AddWithValue("@UserId", currentUserId);
            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync()) return null;

            var content = reader.GetString(2);
            var dto = new ArticleDetailDto
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Content = content,
                Excerpt = content.Length > 130 ? content.Substring(0, 130) + "..." : content,
                Status = reader.GetString(3),
                AuthorId = reader.GetInt32(4),
                AuthorName = reader.GetString(5),
                CategoryId = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                CategoryName = reader.IsDBNull(7) ? null : reader.GetString(7),
                CreatedAt = reader.GetDateTime(8),
                UpdatedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                LikeCount = reader.GetInt32(10),
                IsLikedByUser = reader.GetInt32(11) > 0,
                IsBookmarkedByUser = reader.GetInt32(12) > 0,
                Tags = new List<string>(),
                Comments = new List<CommentDto>()
            };
            reader.Close();

            dto.Tags = await Tag.GetTagsForArticleAsync(conn, id);
            dto.Comments = await CommentDto.FetchForArticleAsync(conn, id);

            return dto;
        }
    }

    // ─── Category DTO with Inline SQL ────────────────────────
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public static async Task<List<CategoryDto>> FetchAllAsync(SqlConnection conn)
        {
            await conn.OpenAsync();
            var sql = "SELECT Id, Name, Description FROM Categories ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<CategoryDto>();
            while (await reader.ReadAsync())
            {
                list.Add(new CategoryDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            return list;
        }
    }

    // ─── Comment DTO with Inline SQL ─────────────────────────
    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public static async Task<List<CommentDto>> FetchForArticleAsync(SqlConnection conn, int articleId)
        {
            var sql = @"SELECT c.Id, c.Content, c.UserId, u.Username AS AuthorName, c.CreatedAt 
                        FROM Comments c 
                        INNER JOIN Users u ON c.UserId = u.Id 
                        WHERE c.ArticleId = @ArticleId 
                        ORDER BY c.CreatedAt ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ArticleId", articleId);
            using var reader = await cmd.ExecuteReaderAsync();
            var comments = new List<CommentDto>();
            while (await reader.ReadAsync())
            {
                comments.Add(new CommentDto
                {
                    Id = reader.GetInt32(0),
                    Content = reader.GetString(1),
                    UserId = reader.GetInt32(2),
                    AuthorName = reader.GetString(3),
                    CreatedAt = reader.GetDateTime(4)
                });
            }
            return comments;
        }
    }

    // ─── Bookmark DTO with Inline SQL ────────────────────────
    public class BookmarkDto
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public ArticleListDto? Article { get; set; }
        public DateTime CreatedAt { get; set; }

        public static async Task<List<BookmarkDto>> FetchUserBookmarksAsync(SqlConnection conn, int userId)
        {
            await conn.OpenAsync();
            var sql = @"
                SELECT b.Id AS BookmarkId, b.CreatedAt AS BookmarkDate,
                       a.Id, a.Title, a.Content, a.Status, a.AuthorId, u.Username AS AuthorName, 
                       a.CategoryId, c.Name AS CategoryName, a.CreatedAt,
                       (SELECT COUNT(*) FROM Likes l WHERE l.ArticleId = a.Id) AS LikeCount
                FROM Bookmarks b
                INNER JOIN Articles a ON b.ArticleId = a.Id
                INNER JOIN Users u ON a.AuthorId = u.Id
                LEFT JOIN Categories c ON a.CategoryId = c.Id
                WHERE b.UserId = @UserId
                ORDER BY b.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<BookmarkDto>();
            while (await reader.ReadAsync())
            {
                var content = reader.GetString(4);
                list.Add(new BookmarkDto
                {
                    Id = reader.GetInt32(0),
                    ArticleId = reader.GetInt32(2),
                    CreatedAt = reader.GetDateTime(1),
                    Article = new ArticleListDto
                    {
                        Id = reader.GetInt32(2),
                        Title = reader.GetString(3),
                        Excerpt = content.Length > 130 ? content.Substring(0, 130) + "..." : content,
                        Status = reader.GetString(5),
                        AuthorId = reader.GetInt32(6),
                        AuthorName = reader.GetString(7),
                        CategoryId = reader.IsDBNull(8) ? null : reader.GetInt32(8),
                        CategoryName = reader.IsDBNull(9) ? null : reader.GetString(9),
                        CreatedAt = reader.GetDateTime(10),
                        LikeCount = reader.GetInt32(11),
                        Tags = new List<string>()
                    }
                });
            }
            reader.Close();

            foreach (var item in list)
            {
                if (item.Article != null)
                    item.Article.Tags = await Tag.GetTagsForArticleAsync(conn, item.ArticleId);
            }

            return list;
        }
    }
}
