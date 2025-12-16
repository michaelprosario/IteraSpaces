using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.Entities;
using AppCore.Services;

namespace IteraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlogsController : ControllerBase
    {
        private readonly IEntityService<Blog> _blogService;

        public BlogsController(IEntityService<Blog> blogService)
        {
            _blogService = blogService;
        }

        /// <summary>
        /// Store a blog (create or update)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> StoreBlog([FromBody] Blog blog)
        {
            // TODO: Get current user ID from authentication context
            var userId = "SYSTEM"; // Placeholder

            var command = new StoreEntityCommand<Blog>(blog)
            {
                UserId = userId
            };

            var result = await _blogService.StoreEntityAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Get blog by ID
        /// </summary>
        [HttpGet("{blogId}")]
        public async Task<IActionResult> GetBlogById(string blogId)
        {
            var query = new GetEntityByIdQuery(blogId);
            var result = await _blogService.GetEntityByIdAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete a blog (soft delete)
        /// </summary>
        [HttpDelete("{blogId}")]
        public async Task<IActionResult> DeleteBlog(string blogId)
        {
            // TODO: Get current user ID from authentication context
            var userId = "SYSTEM"; // Placeholder

            var command = new DeleteEntityCommand(blogId)
            {
                UserId = userId
            };

            var result = await _blogService.DeleteEntityAsync(command);
            return HandleResult(result);
        }

        private IActionResult HandleResult<T>(AppResult<T> result)
        {
            if (result.Success)
            {
                return Ok(result);
            }

            return result.ErrorCode switch
            {
                "VALIDATION_ERROR" => BadRequest(result),
                "ENTITY_NOT_FOUND" => NotFound(result),
                "ENTITY_EXISTS" => Conflict(result),
                _ => BadRequest(result)
            };
        }
    }
}
