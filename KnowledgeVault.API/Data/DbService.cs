using System.Data;
using KnowledgeVault.API.DTOs;
using KnowledgeVault.API.Models;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Data
{
    public class DbService
    {
        private readonly string _connectionString;

        public DbService(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection") 
                ?? "Server=.\\SQLEXPRESS;Database=KnowledgeVaultDb;Trusted_Connection=True;TrustServerCertificate=True;";
        }

        private SqlConnection GetConnection() => new SqlConnection(_connectionString);

        // ─── AUTH (Raw Inline SQL) ───────────────────────────
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "SELECT Id, Username, Email, PasswordHash, Role, CreatedAt FROM Users WHERE Email = @Email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    PasswordHash = reader.GetString("PasswordHash"),
                    Role = reader.GetString("Role"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };
            }
            return null;
        }

        public async Task<int> CreateUserAsync(string username, string email, string passwordHash, string role)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Users (Username, Email, PasswordHash, Role, CreatedAt) 
                        OUTPUT INSERTED.Id 
                        VALUES (@Username, @Email, @PasswordHash, @Role, GETUTCDATE())";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", role);
            var res = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(res);
        }

        public async Task UpdateUserPasswordHashAsync(int userId, string newHash)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Hash", newHash);
            cmd.Parameters.AddWithValue("@Id", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── ARTICLES (Raw Inline SQL) ───────────────────────
        public async Task<List<ArticleListDto>> GetArticlesByStatusAsync(string status)
        {
            using var conn = GetConnection();
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
                var content = reader.GetString("Content");
                var dto = new ArticleListDto
                {
                    Id = reader.GetInt32("Id"),
                    Title = reader.GetString("Title"),
                    Excerpt = content.Length > 130 ? content.Substring(0, 130) + "..." : content,
                    Status = reader.GetString("Status"),
                    AuthorId = reader.GetInt32("AuthorId"),
                    AuthorName = reader.GetString("AuthorName"),
                    CategoryId = reader.IsDBNull("CategoryId") ? null : reader.GetInt32("CategoryId"),
                    CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                    LikeCount = reader.GetInt32("LikeCount"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Tags = new List<string>()
                };
                list.Add(dto);
            }
            reader.Close();

            foreach (var item in list)
            {
                item.Tags = await GetTagsForArticleAsync(conn, item.Id);
            }

            return list;
        }

        public async Task<ArticleDetailDto?> GetArticleByIdAsync(int id, int currentUserId)
        {
            using var conn = GetConnection();
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

            var dto = new ArticleDetailDto
            {
                Id = reader.GetInt32("Id"),
                Title = reader.GetString("Title"),
                Content = reader.GetString("Content"),
                Excerpt = reader.GetString("Content").Length > 130 ? reader.GetString("Content").Substring(0, 130) + "..." : reader.GetString("Content"),
                Status = reader.GetString("Status"),
                AuthorId = reader.GetInt32("AuthorId"),
                AuthorName = reader.GetString("AuthorName"),
                CategoryId = reader.IsDBNull("CategoryId") ? null : reader.GetInt32("CategoryId"),
                CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                LikeCount = reader.GetInt32("LikeCount"),
                IsLikedByUser = reader.GetInt32("UserLiked") > 0,
                IsBookmarkedByUser = reader.GetInt32("UserBookmarked") > 0,
                CreatedAt = reader.GetDateTime("CreatedAt"),
                UpdatedAt = reader.IsDBNull("UpdatedAt") ? null : reader.GetDateTime("UpdatedAt"),
                Tags = new List<string>(),
                Comments = new List<CommentDto>()
            };
            reader.Close();

            dto.Tags = await GetTagsForArticleAsync(conn, id);
            dto.Comments = await GetCommentsForArticleAsync(conn, id);

            return dto;
        }

        public async Task<int> CreateArticleAsync(int authorId, string title, string content, int? categoryId, string status, List<string> tags)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Articles (Title, Content, Status, AuthorId, CategoryId, CreatedAt)
                        OUTPUT INSERTED.Id
                        VALUES (@Title, @Content, @Status, @AuthorId, @CategoryId, GETUTCDATE())";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Content", content);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@AuthorId", authorId);
            cmd.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);

            var res = await cmd.ExecuteScalarAsync();
            int articleId = Convert.ToInt32(res);

            await SaveArticleTagsAsync(conn, articleId, tags);
            return articleId;
        }

        public async Task UpdateArticleAsync(int id, string title, string content, int? categoryId, List<string> tags)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"UPDATE Articles 
                        SET Title = @Title, Content = @Content, CategoryId = @CategoryId, UpdatedAt = GETUTCDATE() 
                        WHERE Id = @Id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Content", content);
            cmd.Parameters.AddWithValue("@CategoryId", (object?)categoryId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();

            var delSql = "DELETE FROM ArticleTags WHERE ArticleId = @ArticleId";
            using var delCmd = new SqlCommand(delSql, conn);
            delCmd.Parameters.AddWithValue("@ArticleId", id);
            await delCmd.ExecuteNonQueryAsync();

            await SaveArticleTagsAsync(conn, id, tags);
        }

        public async Task DeleteArticleAsync(int id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "DELETE FROM Articles WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task ApproveArticleAsync(int id, string status)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "UPDATE Articles SET Status = @Status WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── CATEGORIES (Raw Inline SQL) ─────────────────────
        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "SELECT Id, Name, Description FROM Categories ORDER BY Name";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<CategoryDto>();
            while (await reader.ReadAsync())
            {
                list.Add(new CategoryDto
                {
                    Id = reader.GetInt32("Id"),
                    Name = reader.GetString("Name"),
                    Description = reader.IsDBNull("Description") ? null : reader.GetString("Description")
                });
            }
            return list;
        }

        public async Task<CategoryDto> CreateCategoryAsync(string name, string? description)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Categories (Name, Description) 
                        OUTPUT INSERTED.Id 
                        VALUES (@Name, @Description)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Name", name);
            cmd.Parameters.AddWithValue("@Description", (object?)description ?? DBNull.Value);
            var res = await cmd.ExecuteScalarAsync();
            int id = Convert.ToInt32(res);
            return new CategoryDto { Id = id, Name = name, Description = description };
        }

        public async Task DeleteCategoryAsync(int id)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "DELETE FROM Categories WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── COMMENTS (Raw Inline SQL) ───────────────────────
        public async Task<CommentDto> CreateCommentAsync(int articleId, int userId, string content)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = @"INSERT INTO Comments (ArticleId, UserId, Content, CreatedAt) 
                        OUTPUT INSERTED.Id, INSERTED.CreatedAt 
                        VALUES (@ArticleId, @UserId, @Content, GETUTCDATE())";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ArticleId", articleId);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Content", content);

            using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();
            int commentId = reader.GetInt32(0);
            DateTime createdAt = reader.GetDateTime(1);
            reader.Close();

            var userSql = "SELECT Username FROM Users WHERE Id = @UserId";
            using var userCmd = new SqlCommand(userSql, conn);
            userCmd.Parameters.AddWithValue("@UserId", userId);
            string username = (string)(await userCmd.ExecuteScalarAsync())!;

            return new CommentDto
            {
                Id = commentId,
                Content = content,
                AuthorName = username,
                UserId = userId,
                CreatedAt = createdAt
            };
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "DELETE FROM Comments WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", commentId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── LIKES & BOOKMARKS (Raw Inline SQL) ───────────────
        public async Task<bool> ToggleLikeAsync(int articleId, int userId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var checkSql = "SELECT COUNT(*) FROM Likes WHERE ArticleId = @ArticleId AND UserId = @UserId";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@ArticleId", articleId);
            checkCmd.Parameters.AddWithValue("@UserId", userId);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count > 0)
            {
                var delSql = "DELETE FROM Likes WHERE ArticleId = @ArticleId AND UserId = @UserId";
                using var delCmd = new SqlCommand(delSql, conn);
                delCmd.Parameters.AddWithValue("@ArticleId", articleId);
                delCmd.Parameters.AddWithValue("@UserId", userId);
                await delCmd.ExecuteNonQueryAsync();
                return false;
            }
            else
            {
                var insSql = "INSERT INTO Likes (ArticleId, UserId, CreatedAt) VALUES (@ArticleId, @UserId, GETUTCDATE())";
                using var insCmd = new SqlCommand(insSql, conn);
                insCmd.Parameters.AddWithValue("@ArticleId", articleId);
                insCmd.Parameters.AddWithValue("@UserId", userId);
                await insCmd.ExecuteNonQueryAsync();
                return true;
            }
        }

        public async Task<bool> ToggleBookmarkAsync(int articleId, int userId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var checkSql = "SELECT COUNT(*) FROM Bookmarks WHERE ArticleId = @ArticleId AND UserId = @UserId";
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@ArticleId", articleId);
            checkCmd.Parameters.AddWithValue("@UserId", userId);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

            if (count > 0)
            {
                var delSql = "DELETE FROM Bookmarks WHERE ArticleId = @ArticleId AND UserId = @UserId";
                using var delCmd = new SqlCommand(delSql, conn);
                delCmd.Parameters.AddWithValue("@ArticleId", articleId);
                delCmd.Parameters.AddWithValue("@UserId", userId);
                await delCmd.ExecuteNonQueryAsync();
                return false;
            }
            else
            {
                var insSql = "INSERT INTO Bookmarks (ArticleId, UserId, CreatedAt) VALUES (@ArticleId, @UserId, GETUTCDATE())";
                using var insCmd = new SqlCommand(insSql, conn);
                insCmd.Parameters.AddWithValue("@ArticleId", articleId);
                insCmd.Parameters.AddWithValue("@UserId", userId);
                await insCmd.ExecuteNonQueryAsync();
                return true;
            }
        }

        public async Task<List<BookmarkDto>> GetUserBookmarksAsync(int userId)
        {
            using var conn = GetConnection();
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
                var content = reader.GetString("Content");
                list.Add(new BookmarkDto
                {
                    Id = reader.GetInt32("BookmarkId"),
                    ArticleId = reader.GetInt32("Id"),
                    CreatedAt = reader.GetDateTime("BookmarkDate"),
                    Article = new ArticleListDto
                    {
                        Id = reader.GetInt32("Id"),
                        Title = reader.GetString("Title"),
                        Excerpt = content.Length > 130 ? content.Substring(0, 130) + "..." : content,
                        Status = reader.GetString("Status"),
                        AuthorId = reader.GetInt32("AuthorId"),
                        AuthorName = reader.GetString("AuthorName"),
                        CategoryId = reader.IsDBNull("CategoryId") ? null : reader.GetInt32("CategoryId"),
                        CategoryName = reader.IsDBNull("CategoryName") ? null : reader.GetString("CategoryName"),
                        LikeCount = reader.GetInt32("LikeCount"),
                        CreatedAt = reader.GetDateTime("CreatedAt"),
                        Tags = new List<string>()
                    }
                });
            }
            reader.Close();

            foreach (var item in list)
            {
                if (item.Article != null)
                    item.Article.Tags = await GetTagsForArticleAsync(conn, item.ArticleId);
            }

            return list;
        }

        // ─── USERS (Raw Inline SQL) ───────────────────────────
        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "SELECT Id, Username, Email, Role, CreatedAt FROM Users ORDER BY CreatedAt DESC";
            using var cmd = new SqlCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<UserDto>();
            while (await reader.ReadAsync())
            {
                list.Add(new UserDto
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    Email = reader.GetString("Email"),
                    Role = reader.GetString("Role"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }
            return list;
        }

        public async Task DeleteUserAsync(int userId)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            var sql = "DELETE FROM Users WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        // ─── HELPER INLINE QUERIES ───────────────────────────
        private async Task<List<string>> GetTagsForArticleAsync(SqlConnection conn, int articleId)
        {
            var sql = @"SELECT t.Name 
                        FROM ArticleTags at 
                        INNER JOIN Tags t ON at.TagId = t.Id 
                        WHERE at.ArticleId = @ArticleId";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ArticleId", articleId);
            using var reader = await cmd.ExecuteReaderAsync();
            var tags = new List<string>();
            while (await reader.ReadAsync()) tags.Add(reader.GetString(0));
            return tags;
        }

        private async Task<List<CommentDto>> GetCommentsForArticleAsync(SqlConnection conn, int articleId)
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
                    Id = reader.GetInt32("Id"),
                    Content = reader.GetString("Content"),
                    UserId = reader.GetInt32("UserId"),
                    AuthorName = reader.GetString("AuthorName"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }
            return comments;
        }

        private async Task SaveArticleTagsAsync(SqlConnection conn, int articleId, List<string> tags)
        {
            if (tags == null || !tags.Any()) return;

            foreach (var tagName in tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)))
            {
                var findSql = "SELECT Id FROM Tags WHERE Name = @Name";
                using var findCmd = new SqlCommand(findSql, conn);
                findCmd.Parameters.AddWithValue("@Name", tagName);
                var tagObj = await findCmd.ExecuteScalarAsync();

                int tagId;
                if (tagObj != null)
                {
                    tagId = Convert.ToInt32(tagObj);
                }
                else
                {
                    var insTagSql = "INSERT INTO Tags (Name) OUTPUT INSERTED.Id VALUES (@Name)";
                    using var insTagCmd = new SqlCommand(insTagSql, conn);
                    insTagCmd.Parameters.AddWithValue("@Name", tagName);
                    tagId = Convert.ToInt32(await insTagCmd.ExecuteScalarAsync());
                }

                var linkSql = "INSERT INTO ArticleTags (ArticleId, TagId) VALUES (@ArticleId, @TagId)";
                using var linkCmd = new SqlCommand(linkSql, conn);
                linkCmd.Parameters.AddWithValue("@ArticleId", articleId);
                linkCmd.Parameters.AddWithValue("@TagId", tagId);
                await linkCmd.ExecuteNonQueryAsync();
            }
        }
    }
}
