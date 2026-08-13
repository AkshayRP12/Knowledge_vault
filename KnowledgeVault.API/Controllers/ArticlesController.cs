using System.Security.Claims;
using KnowledgeVault.API.Data;
using KnowledgeVault.API.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ArticlesController : ControllerBase
    {
        private readonly DbService _db;

        public ArticlesController(DbService db)
        {
            _db = db;
        }

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private bool IsAdmin() => User.IsInRole("Admin");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var articles = await _db.GetArticlesByStatusAsync("Approved");
            return Ok(articles);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPending()
        {
            var articles = await _db.GetArticlesByStatusAsync("Pending");
            return Ok(articles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = GetUserId();
            var article = await _db.GetArticleByIdAsync(id, userId);
            if (article == null) return NotFound();
            return Ok(article);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateArticleRequest req)
        {
            var userId = GetUserId();
            string status = IsAdmin() ? "Approved" : "Pending";

            int articleId = await _db.CreateArticleAsync(userId, req.Title, req.Content, req.CategoryId, status, req.Tags);

            return CreatedAtAction(nameof(GetById), new { id = articleId }, new { id = articleId, status });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateArticleRequest req)
        {
            var userId = GetUserId();
            var article = await _db.GetArticleByIdAsync(id, userId);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            await _db.UpdateArticleAsync(id, req.Title, req.Content, req.CategoryId, req.Tags);
            return Ok(new { message = "Article updated" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            var article = await _db.GetArticleByIdAsync(id, userId);
            if (article == null) return NotFound();

            if (article.AuthorId != userId && !IsAdmin()) return Forbid();

            await _db.DeleteArticleAsync(id);
            return Ok(new { message = "Article deleted" });
        }

        [HttpPatch("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveRequest req)
        {
            var userId = GetUserId();
            var article = await _db.GetArticleByIdAsync(id, userId);
            if (article == null) return NotFound();

            await _db.ApproveArticleAsync(id, req.Status);
            return Ok(new { message = $"Article status updated to {req.Status}" });
        }
    }
}
