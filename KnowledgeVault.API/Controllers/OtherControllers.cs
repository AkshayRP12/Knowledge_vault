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
    public class CategoriesController : ControllerBase
    {
        private readonly DbService _db;
        public CategoriesController(DbService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _db.GetCategoriesAsync();
            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
        {
            var category = await _db.CreateCategoryAsync(req.Name, req.Description);
            return Ok(category);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _db.DeleteCategoryAsync(id);
            return Ok(new { message = "Category deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly DbService _db;
        public CommentsController(DbService db) => _db = db;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create(int articleId, [FromBody] CreateCommentRequest req)
        {
            var userId = GetUserId();
            var comment = await _db.CreateCommentAsync(articleId, userId, req.Content);
            return Ok(comment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int articleId, int id)
        {
            await _db.DeleteCommentAsync(id);
            return Ok(new { message = "Comment deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/like")]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly DbService _db;
        public LikesController(DbService db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            bool isLiked = await _db.ToggleLikeAsync(articleId, userId);
            return Ok(new { liked = isLiked });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookmarksController : ControllerBase
    {
        private readonly DbService _db;
        public BookmarksController(DbService db) => _db = db;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            var bookmarks = await _db.GetUserBookmarksAsync(userId);
            return Ok(bookmarks);
        }

        [HttpPost("{articleId}")]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = GetUserId();
            bool isBookmarked = await _db.ToggleBookmarkAsync(articleId, userId);
            return Ok(new { bookmarked = isBookmarked });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly DbService _db;
        public UsersController(DbService db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _db.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _db.DeleteUserAsync(id);
            return Ok(new { message = "User removed" });
        }
    }
}
