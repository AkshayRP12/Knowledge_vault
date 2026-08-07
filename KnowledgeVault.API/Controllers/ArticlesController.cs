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
    public class ArticlesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ArticlesController(AppDbContext db)
        {
            _db = db;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var articles = await _db.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Include(a => a.Likes)
                .Where(a => a.Status == "Approved")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ArticleListDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Excerpt = a.Content.Length > 130 ? a.Content.Substring(0, 130) + "..." : a.Content,
                    Status = a.Status,
                    AuthorName = a.Author.Username,
                    AuthorId = a.AuthorId,
                    CategoryName = a.Category != null ? a.Category.Name : null,
                    CategoryId = a.CategoryId,
                    Tags = a.ArticleTags.Select(at => at.Tag.Name).ToList(),
                    LikeCount = a.Likes.Count,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(articles);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var articles = await _db.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Where(a => a.Status == "Pending")
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new ArticleListDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Excerpt = a.Content.Length > 130 ? a.Content.Substring(0, 130) + "..." : a.Content,
                    Status = a.Status,
                    AuthorName = a.Author.Username,
                    AuthorId = a.AuthorId,
                    CategoryName = a.Category != null ? a.Category.Name : null,
                    CategoryId = a.CategoryId,
                    Tags = a.ArticleTags.Select(at => at.Tag.Name).ToList(),
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(articles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var article = await _db.Articles
                .Include(a => a.Author)
                .Include(a => a.Category)
                .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
                .Include(a => a.Likes)
                .Include(a => a.Bookmarks)
                .Include(a => a.Comments).ThenInclude(c => c.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (article == null) return NotFound();

            var dto = new ArticleDetailDto
            {
                Id = article.Id,
                Title = article.Title,
                Content = article.Content,
                Status = article.Status,
                AuthorName = article.Author.Username,
                AuthorId = article.AuthorId,
                CategoryName = article.Category?.Name,
                CategoryId = article.CategoryId,
                Tags = article.ArticleTags.Select(at => at.Tag.Name).ToList(),
                LikeCount = article.Likes.Count,
                IsLikedByUser = article.Likes.Any(l => l.UserId == userId),
                IsBookmarkedByUser = article.Bookmarks.Any(b => b.UserId == userId),
                CreatedAt = article.CreatedAt,
                UpdatedAt = article.UpdatedAt,
                Comments = article.Comments.OrderBy(c => c.CreatedAt).Select(c => new CommentDto
                {
                    Id = c.Id,
                    Content = c.Content,
                    AuthorName = c.User.Username,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt
                }).ToList()
            };

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateArticleRequest req)
        {
            var userId = GetUserId();
            var article = new Article
            {
                Title = req.Title,
                Content = req.Content,
                CategoryId = req.CategoryId,
                AuthorId = userId,
                Status = IsAdmin() ? "Approved" : "Pending"
            };

            _db.Articles.Add(article);
            await _db.SaveChangesAsync();

            // Tags
            if (req.Tags != null && req.Tags.Any())
            {
                foreach (var tagName in req.Tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)))
                {
                    var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagName };
                        _db.Tags.Add(tag);
                        await _db.SaveChangesAsync();
                    }
                    _db.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, TagId = tag.Id });
                }
                await _db.SaveChangesAsync();
            }

            return CreatedAtAction(nameof(GetById), new { id = article.Id }, new { id = article.Id, status = article.Status });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleRequest req)
        {
            var userId = GetUserId();
            var article = await _db.Articles.Include(a => a.ArticleTags).FirstOrDefaultAsync(a => a.Id == id);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            article.Title = req.Title;
            article.Content = req.Content;
            article.CategoryId = req.CategoryId;
            article.UpdatedAt = DateTime.UtcNow;

            // Sync Tags
            _db.ArticleTags.RemoveRange(article.ArticleTags);
            if (req.Tags != null && req.Tags.Any())
            {
                foreach (var tagName in req.Tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)))
                {
                    var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Name == tagName);
                    if (tag == null)
                    {
                        tag = new Tag { Name = tagName };
                        _db.Tags.Add(tag);
                        await _db.SaveChangesAsync();
                    }
                    _db.ArticleTags.Add(new ArticleTag { ArticleId = article.Id, TagId = tag.Id });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Article updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var article = await _db.Articles.FindAsync(id);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            _db.Articles.Remove(article);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Article deleted" });
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest req)
        {
            var article = await _db.Articles.FindAsync(id);
            if (article == null) return NotFound();

            article.Status = req.Status;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"Article status updated to {req.Status}" });
        }
    }
}
