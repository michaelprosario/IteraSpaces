using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;

namespace IteraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEntityService<UserLoginEvent> _userLoginEventService; 

        public UsersController(
            IUserService userService,
            IEntityService<UserLoginEvent> userLoginEventService
            )
        {
            _userService = userService;
            _userLoginEventService = userLoginEventService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserCommand command)
        {
            var result = await _userService.RegisterUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserById(string userId)
        {
            var query = new GetUserByIdQuery { UserId = userId };
            var result = await _userService.GetUserByIdAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            var query = new GetUserByEmailQuery { Email = email };
            var result = await _userService.GetUserByEmailAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("{userId}/profile")]
        public async Task<IActionResult> UpdateProfile(string userId, [FromBody] UpdateUserProfileCommand command)
        {
            command.UserId = userId;
            var result = await _userService.UpdateUserProfileAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Update user privacy settings
        /// </summary>
        [HttpPut("{userId}/privacy")]
        public async Task<IActionResult> UpdatePrivacySettings(string userId, [FromBody] UserPrivacySettings privacySettings)
        {
            var command = new UpdatePrivacySettingsCommand
            {
                UserId = userId,
                PrivacySettings = privacySettings
            };
            var result = await _userService.UpdatePrivacySettingsAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Disable a user account
        /// </summary>
        [HttpPost("{userId}/disable")]
        public async Task<IActionResult> DisableUser(string userId, [FromBody] DisableUserCommand command)
        {
            command.UserId = userId;
            var result = await _userService.DisableUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Enable a user account
        /// </summary>
        [HttpPost("{userId}/enable")]
        public async Task<IActionResult> EnableUser(string userId)
        {
            // TODO: Get current user ID from authentication context
            var enabledBy = "ADMIN"; // Placeholder
            var result = await _userService.EnableUserAsync(userId, enabledBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Search users
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string searchTerm, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var query = new SearchUsersQuery
            {
                SearchTerm = searchTerm,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
            var result = await _userService.SearchUsersAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Record user login
        /// </summary>
        [HttpPost("{userId}/login")]
        public async Task<IActionResult> RecordLogin(string userId)
        {
            var result = await _userService.RecordLoginAsync(userId);

            // get user agent from the request headers
            var userAgent = Request.Headers["User-Agent"].ToString();

            var loginEvent = new UserLoginEvent
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                UserAgent = userAgent
            };

            // serialize loginEvent for logging
            var loginEventJson = System.Text.Json.JsonSerializer.Serialize(loginEvent);
            Console.WriteLine($"Recording UserLoginEvent: {loginEventJson}");

            var command = new StoreEntityCommand<UserLoginEvent>(loginEvent)
            {
                UserId = userId
            };

            var storeResult = await _userLoginEventService.StoreEntityAsync(command);

            // serialize storeResult errors for logging
            var storeResultJson = System.Text.Json.JsonSerializer.Serialize(storeResult);

            Console.WriteLine($"Store UserLoginEvent result: {storeResultJson}");
            if (!storeResult.Success)
            {
                // Log the error but do not fail the entire request
                throw new Exception($"Failed to store login event: {storeResult.Message}");
            }
            return HandleResult(result);
        }

        /// <summary>
        /// Helper method to handle AppResult and return appropriate HTTP response
        /// </summary>
        private IActionResult HandleResult<T>(AppResult<T> result)
        {
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    data = result.Data,
                    message = result.Message
                });
            }

            // Handle different error types
            if (result.ValidationErrors != null && result.ValidationErrors.Count > 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = result.Message,
                    errorCode = result.ErrorCode,
                    validationErrors = result.ValidationErrors
                });
            }

            // Map error codes to HTTP status codes
            return result.ErrorCode switch
            {
                "USER_NOT_FOUND" => NotFound(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "EMAIL_EXISTS" => Conflict(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "AUTH_FAILED" => Unauthorized(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "INVALID_INPUT" => BadRequest(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                _ => StatusCode(500, new { success = false, message = result.Message, errorCode = result.ErrorCode })
            };
        }
    }
}
