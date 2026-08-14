using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // ─── Inline T-SQL Active Record Methods ───────────────

        public static async Task<List<string>> GetTagsForArticleAsync(SqlConnection conn, int articleId)
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

        public static async Task SaveArticleTagsAsync(SqlConnection conn, int articleId, List<string> tags)
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

    public class ArticleTag
    {
        public int ArticleId { get; set; }
        public int TagId { get; set; }
    }
}
