using System.Data;
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
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
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
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            var sql = "DELETE FROM Comments WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", commentId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<CommentDto>> GetForArticleAsync(SqlConnection conn, int articleId)
        {
            return await CommentDto.FetchForArticleAsync(conn, articleId);
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
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
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
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
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
            return await BookmarkDto.FetchUserBookmarksAsync(conn, userId);
        }
    }
}
