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
        [HttpPost("StoreEntityAsync")]
        public async Task<IActionResult> StoreEntityAsync([FromBody] Blog blog)
        {
            // TODO: Get current user ID from authentication context
            var userId = "SYSTEM"; // Placeholder

            // using validator to validate blog entity
            var validator = new BlogValidator();
            var validationResult = validator.Validate(blog);
            if (!validationResult.IsValid)
            {
                // create list of ValidationError from validationResult
                var errors = validationResult.Errors
                    .Select(e => new ValidationError
                    {
                        PropertyName = e.PropertyName,
                        ErrorMessage = e.ErrorMessage
                    })
                    .ToList();

                return BadRequest(new AppResult<Blog>
                {
                    Success = false,
                    ErrorCode = "VALIDATION_ERROR",
                    ValidationErrors = errors,
                    Message = "Validation failed"
                });
            }

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
        [HttpPost("GetEntityByIdAsync")]
        public async Task<IActionResult> GetEntityByIdAsync([FromBody] GetEntityByIdQuery query)
        {
            var result = await _blogService.GetEntityByIdAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete a blog (soft delete)
        /// </summary>
        [HttpPost("DeleteEntityAsync")]
        public async Task<IActionResult> DeleteEntityAsync([FromBody] DeleteEntityCommand command)
        {
            // TODO: Get current user ID from authentication context
            var userId = "SYSTEM"; // Placeholder
            command.UserId = userId;

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
