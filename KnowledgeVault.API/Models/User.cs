using KnowledgeVault.API.DTOs;
using Microsoft.Data.SqlClient;

namespace KnowledgeVault.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ─── Inline T-SQL Active Record Methods ───────────────

        public static async Task<User?> GetByEmailAsync(SqlConnection conn, string email)
        {
            await conn.OpenAsync();
            var sql = "SELECT Id, Username, Email, PasswordHash, Role, CreatedAt FROM Users WHERE Email = @Email";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    Email = reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    Role = reader.GetString(4),
                    CreatedAt = reader.GetDateTime(5)
                };
            }
            return null;
        }

        public static async Task<int> CreateAsync(SqlConnection conn, string username, string email, string passwordHash, string role)
        {
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

        public static async Task UpdatePasswordHashAsync(SqlConnection conn, int userId, string newHash)
        {
            await conn.OpenAsync();
            var sql = "UPDATE Users SET PasswordHash = @Hash WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Hash", newHash);
            cmd.Parameters.AddWithValue("@Id", userId);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<List<UserDto>> GetAllAsync(SqlConnection conn)
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

        public static async Task DeleteAsync(SqlConnection conn, int userId)
        {
            await conn.OpenAsync();
            var sql = "DELETE FROM Users WHERE Id = @Id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", userId);
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
