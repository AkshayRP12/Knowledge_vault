using System.Security.Claims;
using KnowledgeVault.API.Data;
using KnowledgeVault.API.DTOs;
using KnowledgeVault.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CategoriesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _db.Categories
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name, Description = c.Description })
                .ToListAsync();
            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
        {
            var category = new Category { Name = req.Name, Description = req.Description };
            _db.Categories.Add(category);
            await _db.SaveChangesAsync();
            return Ok(new CategoryDto { Id = category.Id, Name = category.Name, Description = category.Description });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var cat = await _db.Categories.FindAsync(id);
            if (cat == null) return NotFound();
            _db.Categories.Remove(cat);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Category deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly AppDbContext _db;
        public CommentsController(AppDbContext db) => _db = db;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create(int articleId, [FromBody] CreateCommentRequest req)
        {
            var userId = GetUserId();
            var comment = new Comment
            {
                ArticleId = articleId,
                UserId = userId,
                Content = req.Content,
                CreatedAt = DateTime.UtcNow
            };
            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();

            var user = await _db.Users.FindAsync(userId);
            return Ok(new CommentDto
            {
                Id = comment.Id,
                Content = comment.Content,
                AuthorName = user?.Username ?? "User",
                UserId = userId,
                CreatedAt = comment.CreatedAt
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int articleId, int id)
        {
            var userId = GetUserId();
            var comment = await _db.Comments.FindAsync(id);
            if (comment == null) return NotFound();
            if (comment.UserId != userId && !User.IsInRole("Admin")) return Forbid();

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Comment deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/like")]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly AppDbContext _db;
        public LikesController(AppDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var like = await _db.Likes.FirstOrDefaultAsync(l => l.ArticleId == articleId && l.UserId == userId);

            if (like != null)
            {
                _db.Likes.Remove(like);
                await _db.SaveChangesAsync();
                return Ok(new { liked = false });
            }
            else
            {
                _db.Likes.Add(new Like { ArticleId = articleId, UserId = userId });
                await _db.SaveChangesAsync();
                return Ok(new { liked = true });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookmarksController : ControllerBase
    {
        private readonly AppDbContext _db;
        public BookmarksController(AppDbContext db) => _db = db;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var bookmarks = await _db.Bookmarks
                .Include(b => b.Article).ThenInclude(a => a.Author)
                .Include(b => b.Article).ThenInclude(a => a.Category)
                .Include(b => b.Article).ThenInclude(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Include(b => b.Article).ThenInclude(a => a.Likes)
                .Where(b => b.UserId == userId)
                .Select(b => new BookmarkDto
                {
                    Id = b.Id,
                    ArticleId = b.ArticleId,
                    CreatedAt = b.CreatedAt,
                    Article = new ArticleListDto
                    {
                        Id = b.Article.Id,
                        Title = b.Article.Title,
                        Excerpt = b.Article.Content.Length > 130 ? b.Article.Content.Substring(0, 130) + "..." : b.Article.Content,
                        Status = b.Article.Status,
                        AuthorName = b.Article.Author.Username,
                        AuthorId = b.Article.AuthorId,
                        CategoryName = b.Article.Category != null ? b.Article.Category.Name : null,
                        CategoryId = b.Article.CategoryId,
                        Tags = b.Article.ArticleTags.Select(at => at.Tag.Name).ToList(),
                        LikeCount = b.Article.Likes.Count,
                        CreatedAt = b.Article.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(bookmarks);
        }

        [HttpPost("{articleId}")]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = GetUserId();
            var bookmark = await _db.Bookmarks.FirstOrDefaultAsync(b => b.ArticleId == articleId && b.UserId == userId);

            if (bookmark != null)
            {
                _db.Bookmarks.Remove(bookmark);
                await _db.SaveChangesAsync();
                return Ok(new { bookmarked = false });
            }
            else
            {
                _db.Bookmarks.Add(new Bookmark { ArticleId = articleId, UserId = userId });
                await _db.SaveChangesAsync();
                return Ok(new { bookmarked = true });
            }
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();
            return Ok(users);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null) return NotFound();
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return Ok(new { message = "User removed" });
        }
    }
}
