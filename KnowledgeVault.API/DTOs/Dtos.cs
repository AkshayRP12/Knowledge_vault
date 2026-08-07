namespace KnowledgeVault.API.DTOs
{
    // ─── Auth ───────────────────────────────────────────────
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Employee";
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    // ─── User ───────────────────────────────────────────────
    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // ─── Article ────────────────────────────────────────────
    public class CreateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class UpdateArticleRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
    }

    public class ApproveRequest
    {
        public string Status { get; set; } = string.Empty; // Approved | Rejected
    }

    public class ArticleListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Excerpt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string? CategoryName { get; set; }
        public int? CategoryId { get; set; }
        public List<string> Tags { get; set; } = new();
        public int LikeCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ArticleDetailDto : ArticleListDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsLikedByUser { get; set; }
        public bool IsBookmarkedByUser { get; set; }
        public List<CommentDto> Comments { get; set; } = new();
        public DateTime? UpdatedAt { get; set; }
    }

    // ─── Category ───────────────────────────────────────────
    public class CreateCategoryRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    // ─── Comment ────────────────────────────────────────────
    public class CreateCommentRequest
    {
        public string Content { get; set; } = string.Empty;
    }

    public class CommentDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ─── Bookmark ───────────────────────────────────────────
    public class BookmarkDto
    {
        public int Id { get; set; }
        public int ArticleId { get; set; }
        public ArticleListDto? Article { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
