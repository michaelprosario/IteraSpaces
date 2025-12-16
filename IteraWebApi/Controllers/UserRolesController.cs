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
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRoleService _userRoleService;
        private readonly IRoleService _roleService;

        public UserRolesController(IUserRoleService userRoleService, IRoleService roleService)
        {
            _userRoleService = userRoleService;
            _roleService = roleService;
        }

        /// <summary>
        /// Get all roles for a specific user
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <returns>List of roles assigned to the user</returns>
        [HttpGet("users/{userId}/roles")]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            var query = new GetUserRolesQuery { UserId = userId };
            var result = await _userRoleService.GetUserRolesAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Check if a user has a specific role
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="roleId">The role ID</param>
        /// <returns>Boolean indicating if user has the role</returns>
        [HttpGet("users/{userId}/roles/{roleId}")]
        public async Task<IActionResult> UserHasRole(string userId, string roleId)
        {
            var result = await _userRoleService.UserHasRoleAsync(userId, roleId);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all available roles in the system
        /// </summary>
        /// <returns>List of all roles</returns>
        [HttpGet("roles")]
        public async Task<IActionResult> GetAllRoles()
        {
            var result = await _roleService.GetAllRolesAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Assign a role to a user
        /// </summary>
        /// <param name="command">Assignment details</param>
        /// <returns>The created user role association</returns>
        [HttpPost("users/roles")]
        public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleToUserCommand command)
        {
            var result = await _userRoleService.AssignRoleToUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Remove a role from a user
        /// </summary>
        /// <param name="command">Removal details</param>
        /// <returns>Success indicator</returns>
        [HttpDelete("users/roles")]
        public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleFromUserCommand command)
        {
            var result = await _userRoleService.RemoveRoleFromUserAsync(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Get all users that have a specific role
        /// </summary>
        /// <param name="roleId">The role ID</param>
        /// <returns>List of users with the specified role</returns>
        [HttpGet("roles/{roleId}/users")]
        public async Task<IActionResult> GetUsersInRole(string roleId)
        {
            var query = new GetUsersInRoleQuery { RoleId = roleId };
            var result = await _userRoleService.GetUsersInRoleAsync(query);
            return HandleResult(result);
        }

        /// <summary>
        /// Helper method to convert AppResult to appropriate HTTP response
        /// </summary>
        private IActionResult HandleResult<T>(AppResult<T> result)
        {
            if (result.Success)
            {
                return Ok(new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
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
                "ROLE_NOT_FOUND" => NotFound(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "ROLE_ALREADY_ASSIGNED" => Conflict(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "ROLE_NOT_ASSIGNED" => NotFound(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                "INVALID_INPUT" => BadRequest(new { success = false, message = result.Message, errorCode = result.ErrorCode }),
                _ => StatusCode(500, new { success = false, message = result.Message, errorCode = result.ErrorCode })
            };
        }
    }
}
