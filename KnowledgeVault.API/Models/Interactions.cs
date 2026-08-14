using KnowledgeVault.API.DTOs;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ArticleId { get; set; }
        public int UserId { get; set; }

        public static async Task<CommentDto> CreateAsync(SqlConnection conn, int articleId, int userId, string content)
        {
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

        public static async Task DeleteAsync(SqlConnection conn, int commentId)
        {
            await conn.OpenAsync();
            var sql = "DELETE FROM Comments WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", commentId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<CommentDto>> GetForArticleAsync(SqlConnection conn, int articleId)
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

    public class Like
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ArticleId { get; set; }
        public int UserId { get; set; }

        public static async Task<bool> ToggleAsync(SqlConnection conn, int articleId, int userId)
        {
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
    }

    public class Bookmark
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int ArticleId { get; set; }
        public int UserId { get; set; }

        public static async Task<bool> ToggleAsync(SqlConnection conn, int articleId, int userId)
        {
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

        public static async Task<List<BookmarkDto>> GetUserBookmarksAsync(SqlConnection conn, int userId)
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
