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

        public static async Task<ArticleDetailDto?> GetByIdAsync(SqlConnection conn, int id, int currentUserId)
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
            dto.Comments = await Comment.GetForArticleAsync(conn, id);

            return dto;
        }

        public static async Task<int> CreateAsync(SqlConnection conn, int authorId, string title, string content, int? categoryId, string status, List<string> tags)
        {
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

            await Tag.SaveArticleTagsAsync(conn, articleId, tags);
            return articleId;
        }

        public static async Task UpdateAsync(SqlConnection conn, int id, string title, string content, int? categoryId, List<string> tags)
        {
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

            await Tag.SaveArticleTagsAsync(conn, id, tags);
        }

        public static async Task DeleteAsync(SqlConnection conn, int id)
        {
            await conn.OpenAsync();
            var sql = "DELETE FROM Articles WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task ApproveAsync(SqlConnection conn, int id, string status)
        {
            await conn.OpenAsync();
            var sql = "UPDATE Articles SET Status = @Status WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
