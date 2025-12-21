using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using Marten;

namespace IteraWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEntityService<UserLoginEvent> _userLoginEventService;
        private readonly IUsersQueryService _usersQueryService;

        public UsersController(
            IUserService userService,
            IEntityService<UserLoginEvent> userLoginEventService,
            IUsersQueryService usersQueryService
            )
        {
            _userService = userService;
            _userLoginEventService = userLoginEventService;
            _usersQueryService = usersQueryService;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("RegisterUserAsync")]
        public async Task<IActionResult> RegisterUserAsync([FromBody] RegisterUserCommand command)
        {
            var result = await _userService.RegisterUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by ID
        /// </summary>
        [HttpPost("GetUserByIdAsync")]
        public async Task<IActionResult> GetUserByIdAsync([FromBody] GetUserByIdQuery query)
        {
            var result = await _userService.GetUserByIdAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Get user by email
        /// </summary>
        [HttpPost("GetUserByEmailAsync")]
        public async Task<IActionResult> GetUserByEmailAsync([FromBody] GetUserByEmailQuery query)
        {
            var result = await _userService.GetUserByEmailAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPost("UpdateUserProfileAsync")]
        public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateUserProfileCommand command)
        {
            var result = await _userService.UpdateUserProfileAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Update user privacy settings
        /// </summary>
        [HttpPost("UpdatePrivacySettingsAsync")]
        public async Task<IActionResult> UpdatePrivacySettingsAsync([FromBody] UpdatePrivacySettingsCommand command)
        {
            var result = await _userService.UpdatePrivacySettingsAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Disable a user account
        /// </summary>
        [HttpPost("DisableUserAsync")]
        public async Task<IActionResult> DisableUserAsync([FromBody] DisableUserCommand command)
        {
            var result = await _userService.DisableUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Enable a user account
        /// </summary>
        [HttpPost("EnableUserAsync")]
        public async Task<IActionResult> EnableUserAsync([FromBody] EnableUserCommand command)
        {
            var result = await _userService.EnableUserAsync(command.UserId, command.EnabledBy);
            return HandleResult(result);
        }

        /// <summary>
        /// Search users
        /// </summary>
        [HttpPost("GetUsersAsync")]
        public async Task<PagedResults<User>> GetUsersAsync([FromBody] SearchQuery query)
        {
            return await _usersQueryService.GetUsersAsync(query);
        }

        /// <summary>
        /// Record user login
        /// </summary>
        [HttpPost("RecordLoginAsync")]
        public async Task<IActionResult> RecordLoginAsync([FromBody] RecordLoginCommand command)
        {
            var result = await _userService.RecordLoginAsync(command.UserId);

            // get user agent from the request headers
            var userAgent = Request.Headers["User-Agent"].ToString();

            var loginEvent = new UserLoginEvent
            {
                Id = Guid.NewGuid().ToString(),
                UserId = command.UserId,
                UserAgent = userAgent
            };


            var storeCommand = new StoreEntityCommand<UserLoginEvent>(loginEvent)
            {
                UserId = command.UserId
            };

            var storeResult = await _userLoginEventService.StoreEntityAsync(storeCommand);

            if (!storeResult.Success)
            {
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
