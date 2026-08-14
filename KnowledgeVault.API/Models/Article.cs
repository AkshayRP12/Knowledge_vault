using System.Data;
using KnowledgeVault.API.DTOs;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public int AuthorId { get; set; }
        public int? CategoryId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // ─── Inline T-SQL Active Record Methods ───────────────

        public static async Task<List<ArticleListDto>> GetByStatusAsync(SqlConnection conn, string status)
        {
            return await ArticleListDto.FetchByStatusAsync(conn, status);
        }

        public static async Task<ArticleDetailDto?> GetByIdAsync(SqlConnection conn, int id, int currentUserId)
        {
            return await ArticleDetailDto.FetchByIdAsync(conn, id, currentUserId);
        }

        public static async Task<int> CreateAsync(SqlConnection conn, int authorId, string title, string content, int? categoryId, string status, List<string> tags)
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
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

            await Tag.SaveArticleTagsAsync(conn, articleId, tags);
            return articleId;
        }

        public static async Task UpdateAsync(SqlConnection conn, int id, string title, string content, int? categoryId, List<string> tags)
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
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

            await Tag.SaveArticleTagsAsync(conn, id, tags);
        }

        public static async Task DeleteAsync(SqlConnection conn, int id)
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            var sql = "DELETE FROM Articles WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ApproveAsync(SqlConnection conn, int id, string status)
        {
            if (conn.State != ConnectionState.Open) await conn.OpenAsync();
            var sql = "UPDATE Articles SET Status = @Status WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
