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
    public interface IRoleService
    {
        Task<AppResult<Role>> CreateRoleAsync(CreateRoleCommand command);
        Task<AppResult<Role>> GetRoleByIdAsync(GetRoleByIdQuery query);
        Task<AppResult<Role>> GetRoleByNameAsync(GetRoleByNameQuery query);
        Task<AppResult<List<Role>>> GetAllRolesAsync();
        Task<AppResult<Role>> UpdateRoleAsync(UpdateRoleCommand command);
        Task<AppResult<bool>> DeleteRoleAsync(DeleteRoleCommand command);
    }

    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<AppResult<Role>> CreateRoleAsync(CreateRoleCommand command)
        {
            // Validation
            var validationErrors = ValidateCreateRole(command);
            if (validationErrors.Any())
                return AppResult<Role>.ValidationFailure(validationErrors);

            // Check if role name already exists
            if (await _roleRepository.RoleExistsAsync(command.Name))
                return AppResult<Role>.FailureResult("Role name already exists", "ROLE_EXISTS");

            // Create role entity
            var role = new Role
            {
                Id = Guid.NewGuid().ToString(),
                Name = command.Name,
                Description = command.Description,
                IsSystemRole = command.IsSystemRole,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.CreatedBy
            };

            // Save to repository
            var savedRole = _roleRepository.Add(role);

            return AppResult<Role>.SuccessResult(savedRole, "Role created successfully");
        }

        public async Task<AppResult<Role>> GetRoleByIdAsync(GetRoleByIdQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.RoleId))
                return AppResult<Role>.FailureResult("Role ID is required", "INVALID_INPUT");

            var role = _roleRepository.GetById(query.RoleId);
            if (role == null)
                return AppResult<Role>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            return AppResult<Role>.SuccessResult(role);
        }

        public async Task<AppResult<Role>> GetRoleByNameAsync(GetRoleByNameQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.Name))
                return AppResult<Role>.FailureResult("Role name is required", "INVALID_INPUT");

            var role = await _roleRepository.GetByNameAsync(query.Name);
            if (role == null)
                return AppResult<Role>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            return AppResult<Role>.SuccessResult(role);
        }

        public async Task<AppResult<List<Role>>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllRolesAsync();
            return AppResult<List<Role>>.SuccessResult(roles, $"Found {roles.Count} roles");
        }

        public async Task<AppResult<Role>> UpdateRoleAsync(UpdateRoleCommand command)
        {
            // Validation
            var validationErrors = ValidateUpdateRole(command);
            if (validationErrors.Any())
                return AppResult<Role>.ValidationFailure(validationErrors);

            // Get existing role
            var role = _roleRepository.GetById(command.RoleId);
            if (role == null)
                return AppResult<Role>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            // Check if it's a system role
            if (role.IsSystemRole)
                return AppResult<Role>.FailureResult("Cannot update system role", "SYSTEM_ROLE");

            // Update properties
            if (!string.IsNullOrWhiteSpace(command.Name))
            {
                // Check if new name already exists
                var existingRole = await _roleRepository.GetByNameAsync(command.Name);
                if (existingRole != null && existingRole.Id != role.Id)
                    return AppResult<Role>.FailureResult("Role name already exists", "ROLE_EXISTS");

                role.Name = command.Name;
            }

            if (!string.IsNullOrWhiteSpace(command.Description))
                role.Description = command.Description;

            role.UpdatedAt = DateTime.UtcNow;
            role.UpdatedBy = command.UpdatedBy;

            // Save changes
            _roleRepository.Update(role);

            return AppResult<Role>.SuccessResult(role, "Role updated successfully");
        }

        public async Task<AppResult<bool>> DeleteRoleAsync(DeleteRoleCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.RoleId))
                return AppResult<bool>.FailureResult("Role ID is required", "INVALID_INPUT");

            var role = _roleRepository.GetById(command.RoleId);
            if (role == null)
                return AppResult<bool>.FailureResult("Role not found", "ROLE_NOT_FOUND");

            // Check if it's a system role
            if (role.IsSystemRole)
                return AppResult<bool>.FailureResult("Cannot delete system role", "SYSTEM_ROLE");

            // Soft delete
            role.IsDeleted = true;
            role.DeletedAt = DateTime.UtcNow;
            role.DeletedBy = command.DeletedBy;

            _roleRepository.Update(role);

            return AppResult<bool>.SuccessResult(true, "Role deleted successfully");
        }

        private List<ValidationError> ValidateCreateRole(CreateRoleCommand command)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.Name))
                errors.Add(new ValidationError { PropertyName = "Name", ErrorMessage = "Role name is required" });

            if (string.IsNullOrWhiteSpace(command.Description))
                errors.Add(new ValidationError { PropertyName = "Description", ErrorMessage = "Role description is required" });

            if (string.IsNullOrWhiteSpace(command.CreatedBy))
                errors.Add(new ValidationError { PropertyName = "CreatedBy", ErrorMessage = "CreatedBy is required" });

            return errors;
        }

        private List<ValidationError> ValidateUpdateRole(UpdateRoleCommand command)
        {
            var errors = new List<ValidationError>();

            if (string.IsNullOrWhiteSpace(command.RoleId))
                errors.Add(new ValidationError { PropertyName = "RoleId", ErrorMessage = "Role ID is required" });

            if (string.IsNullOrWhiteSpace(command.UpdatedBy))
                errors.Add(new ValidationError { PropertyName = "UpdatedBy", ErrorMessage = "UpdatedBy is required" });

            return errors;
        }
    }
}
