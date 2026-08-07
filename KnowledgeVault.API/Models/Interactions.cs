namespace KnowledgeVault.API.Models
{
    public class Comment
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

    public class Like
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

    public class Bookmark
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}
