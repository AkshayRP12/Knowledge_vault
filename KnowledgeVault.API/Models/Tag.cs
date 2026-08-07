namespace KnowledgeVault.API.Models
{
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<ArticleTag> ArticleTags { get; set; } = new List<ArticleTag>();
    }

    public class ArticleTag
    {
        public int ArticleId { get; set; }
        public Article Article { get; set; } = null!;
        public int TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }
}
