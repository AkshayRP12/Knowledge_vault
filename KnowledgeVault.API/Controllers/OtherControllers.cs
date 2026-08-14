using System.Security.Claims;
using KnowledgeVault.API.Data;
using KnowledgeVault.API.DTOs;
using KnowledgeVault.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AppUser = KnowledgeVault.API.Models.User;

namespace KnowledgeVault.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        public CategoriesController(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _dbFactory.CreateConnection();
            var categories = await CategoryDto.FetchAllAsync(conn);
            return Ok(categories);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
        {
            using var conn = _dbFactory.CreateConnection();
            var category = await Category.CreateAsync(conn, req.Name, req.Description);
            return Ok(category);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _dbFactory.CreateConnection();
            await Category.DeleteAsync(conn, id);
            return Ok(new { message = "Category deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/comments")]
    [Authorize]
    public class CommentsController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        public CommentsController(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost]
        public async Task<IActionResult> Create(int articleId, [FromBody] CreateCommentRequest req)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var comment = await Comment.CreateAsync(conn, articleId, userId, req.Content);
            return Ok(comment);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int articleId, int id)
        {
            using var conn = _dbFactory.CreateConnection();
            await Comment.DeleteAsync(conn, id);
            return Ok(new { message = "Comment deleted" });
        }
    }

    [ApiController]
    [Route("api/articles/{articleId}/like")]
    [Authorize]
    public class LikesController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        public LikesController(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        [HttpPost]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            using var conn = _dbFactory.CreateConnection();
            bool isLiked = await Like.ToggleAsync(conn, articleId, userId);
            return Ok(new { liked = isLiked });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookmarksController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        public BookmarksController(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            var bookmarks = await BookmarkDto.FetchUserBookmarksAsync(conn, userId);
            return Ok(bookmarks);
        }

        [HttpPost("{articleId}")]
        public async Task<IActionResult> Toggle(int articleId)
        {
            var userId = GetUserId();
            using var conn = _dbFactory.CreateConnection();
            bool isBookmarked = await Bookmark.ToggleAsync(conn, articleId, userId);
            return Ok(new { bookmarked = isBookmarked });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class UsersController : ControllerBase
    {
        private readonly DbConnectionFactory _dbFactory;
        public UsersController(DbConnectionFactory dbFactory) => _dbFactory = dbFactory;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = _dbFactory.CreateConnection();
            var users = await UserDto.FetchAllAsync(conn);
            return Ok(users);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            using var conn = _dbFactory.CreateConnection();
            await AppUser.DeleteAsync(conn, id);
            return Ok(new { message = "User removed" });
        }
    }
}
