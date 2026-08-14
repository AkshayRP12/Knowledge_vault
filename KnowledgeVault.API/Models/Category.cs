using KnowledgeVault.API.DTOs;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // ─── Inline T-SQL Active Record Methods ───────────────

        public static async Task<List<CategoryDto>> GetAllAsync(SqlConnection conn)
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

        public static async Task<CategoryDto> CreateAsync(SqlConnection conn, string name, string? description)
        {
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

        public static async Task DeleteAsync(SqlConnection conn, int id)
        {
            await conn.OpenAsync();
            var sql = "DELETE FROM Categories WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
