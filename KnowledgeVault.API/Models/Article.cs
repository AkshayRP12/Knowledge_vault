namespace KnowledgeVault.API.Models
{
    public class Article
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending | Approved | Rejected
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Foreign Keys
        public int AuthorId { get; set; }
        public int? CategoryId { get; set; }

        // Navigation
        public User Author { get; set; } = null!;
        public Category? Category { get; set; }
        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Bookmark> Bookmarks { get; set; } = new List<Bookmark>();
    }
}
