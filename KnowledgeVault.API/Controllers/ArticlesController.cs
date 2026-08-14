using System.Security.Claims;
using KnowledgeVault.API.Data;
using KnowledgeVault.API.DTOs;
using KnowledgeVault.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArticlesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;

        public ArticlesController(DbConnectionFactory dbFactory)
        {
            _dbFactory = dbFactory;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _dbFactory.CreateConnection();
            var articles = await ArticleListDto.FetchByStatusAsync(conn, "Approved");
            return Ok(articles);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            using var conn = _dbFactory.CreateConnection();
            var articles = await ArticleListDto.FetchByStatusAsync(conn, "Pending");
            return Ok(articles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var article = await ArticleDetailDto.FetchByIdAsync(conn, id, userId);
            if (article == null) return NotFound();
            return Ok(article);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateArticleRequest req)
        {
            var userId = GetUserId();
            string status = IsAdmin() ? "Approved" : "Pending";

            using var conn = _dbFactory.CreateConnection();
            int articleId = await Article.CreateAsync(conn, userId, req.Title, req.Content, req.CategoryId, status, req.Tags);

            return CreatedAtAction(nameof(GetById), new { id = articleId }, new { id = articleId, status });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleRequest req)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var article = await ArticleDetailDto.FetchByIdAsync(conn, id, userId);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            await Article.UpdateAsync(conn, id, req.Title, req.Content, req.CategoryId, req.Tags);
            return Ok(new { message = "Article updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var article = await ArticleDetailDto.FetchByIdAsync(conn, id, userId);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            await Article.DeleteAsync(conn, id);
            return Ok(new { message = "Article deleted" });
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest req)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var article = await ArticleDetailDto.FetchByIdAsync(conn, id, userId);
            if (article == null) return NotFound();

            await Article.ApproveAsync(conn, id, req.Status);
            return Ok(new { message = $"Article status updated to {req.Status}" });
        }
    }
}
