using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services
{
    public interface IUserRoleService
    {
        Task<AppResult<UserRole>> AssignRoleToUserAsync(AssignRoleToUserCommand command);
        Task<AppResult<bool>> RemoveRoleFromUserAsync(RemoveRoleFromUserCommand command);
        Task<AppResult<List<Role>>> GetUserRolesAsync(GetUserRolesQuery query);
        Task<AppResult<List<string>>> GetUserRoleNamesAsync(GetUserRolesQuery query);
        Task<AppResult<bool>> UserHasRoleAsync(string userId, string roleId);
        Task<AppResult<List<User>>> GetUsersInRoleAsync(GetUsersInRoleQuery query);
    }

    public class UserRoleService : IUserRoleService
    {
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;

        public UserRoleService(
            IUserRoleRepository userRoleRepository,
            IUserRepository userRepository,
            IRoleRepository roleRepository)
        {
            _userRoleRepository = userRoleRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
        }

        public async Task<AppResult<UserRole>> AssignRoleToUserAsync(AssignRoleToUserCommand command)
        {
            // Validation
            var validationErrors = ValidateAssignRole(command);
            if (validationErrors.Any())
                return AppResult<UserRole>.ValidationFailure(validationErrors);

            // Check if user exists
            var user = await _userRepository.GetById(command.UserId);
            if (user == null)
                return AppResult<UserRole>.FailureResult("User not found", "USER_NOT_FOUND");

            // Check if role exists
            var role = await _roleRepository.GetById(command.RoleId);
            if (role == null)
                return AppResult<UserRole>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            // Check if user already has this role
            if (await _userRoleRepository.UserHasRoleAsync(command.UserId, command.RoleId))
                return AppResult<UserRole>.FailureResult("User already has this role", "ROLE_ALREADY_ASSIGNED");

            // Create user role
            var userRole = new UserRole
            {
                Id = Guid.NewGuid().ToString(),
                UserId = command.UserId,
                RoleId = command.RoleId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.AssignedBy
            };

            // Save to repository
            var savedUserRole = await _userRoleRepository.Add(userRole);

            return AppResult<UserRole>.SuccessResult(savedUserRole, "Role assigned to user successfully");
        }

        public async Task<AppResult<bool>> RemoveRoleFromUserAsync(RemoveRoleFromUserCommand command)
        {
            // Validation
            var validationErrors = ValidateRemoveRole(command);
            if (validationErrors.Any())
                return AppResult<bool>.ValidationFailure(validationErrors);

            // Get user role
            var userRole = await _userRoleRepository.GetUserRoleAsync(command.UserId, command.RoleId);
            if (userRole == null)
                return AppResult<bool>.FailureResult("User does not have this role", "ROLE_NOT_ASSIGNED");

            // Soft delete - set DeletedBy before calling Delete
            userRole.DeletedBy = command.RemovedBy;
            await _userRoleRepository.Delete(userRole);

            return AppResult<bool>.SuccessResult(true, "Role removed from user successfully");
        }

        public async Task<AppResult<List<Role>>> GetUserRolesAsync(GetUserRolesQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.UserId))
                return AppResult<List<Role>>.FailureResult("User ID is required", "INVALID_INPUT");

            // Check if user exists
            var user = await _userRepository.GetById(query.UserId);
            if (user == null)
                return AppResult<List<Role>>.FailureResult("User not found", "USER_NOT_FOUND");

            var userRoles = await _userRoleRepository.GetUserRolesAsync(query.UserId);
            var roles = userRoles.Select(ur => ur.Role).ToList();

            return AppResult<List<Role>>.SuccessResult(roles, $"Found {roles.Count} roles for user");
        }

        public async Task<AppResult<List<string>>> GetUserRoleNamesAsync(GetUserRolesQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.UserId))
                return AppResult<List<string>>.FailureResult("User ID is required", "INVALID_INPUT");

            // Check if user exists
            var user = await _userRepository.GetById(query.UserId);
            if (user == null)
                return AppResult<List<string>>.FailureResult("User not found", "USER_NOT_FOUND");

            var roleNames = await _userRoleRepository.GetUserRoleNamesAsync(query.UserId);

            return AppResult<List<string>>.SuccessResult(roleNames, $"Found {roleNames.Count} roles for user");
        }

        public async Task<AppResult<bool>> UserHasRoleAsync(string userId, string roleId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return AppResult<bool>.FailureResult("User ID is required", "INVALID_INPUT");

            if (string.IsNullOrWhiteSpace(roleId))
                return AppResult<bool>.FailureResult("Role ID is required", "INVALID_INPUT");

            var hasRole = await _userRoleRepository.UserHasRoleAsync(userId, roleId);

            return AppResult<bool>.SuccessResult(hasRole);
        }

        public async Task<AppResult<List<User>>> GetUsersInRoleAsync(GetUsersInRoleQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.RoleId))
                return AppResult<List<User>>.FailureResult("Role ID is required", "INVALID_INPUT");

            // Check if role exists
            var role = await _roleRepository.GetById(query.RoleId);
            if (role == null)
                return AppResult<List<User>>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            var users = await _userRoleRepository.GetUsersInRoleAsync(query.RoleId);

            return AppResult<List<User>>.SuccessResult(users, $"Found {users.Count} users in role");
        }

        private List<ValidationError> ValidateAssignRole(AssignRoleToUserCommand command)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.UserId))
                errors.Add(new ValidationError { PropertyName = "UserId", ErrorMessage = "User ID is required" });

            if (string.IsNullOrWhiteSpace(command.RoleId))
                errors.Add(new ValidationError { PropertyName = "RoleId", ErrorMessage = "Role ID is required" });

            if (string.IsNullOrWhiteSpace(command.AssignedBy))
                errors.Add(new ValidationError { PropertyName = "AssignedBy", ErrorMessage = "AssignedBy is required" });

            return errors;
        }

        private List<ValidationError> ValidateRemoveRole(RemoveRoleFromUserCommand command)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.UserId))
                errors.Add(new ValidationError { PropertyName = "UserId", ErrorMessage = "User ID is required" });

            if (string.IsNullOrWhiteSpace(command.RoleId))
                errors.Add(new ValidationError { PropertyName = "RoleId", ErrorMessage = "Role ID is required" });

            if (string.IsNullOrWhiteSpace(command.RemovedBy))
                errors.Add(new ValidationError { PropertyName = "RemovedBy", ErrorMessage = "RemovedBy is required" });

            return errors;
        }
    }
}
