using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using NSubstitute;
using NUnit.Framework;

namespace AppCore.UnitTests.Services
{
    [TestFixture]
    public class RoleServiceTests
    {
        private IRoleRepository _roleRepository;
        private RoleService _roleService;

        [SetUp]
        public void Setup()
        {
            _roleRepository = Substitute.For<IRoleRepository>();
            _roleService = new RoleService(_roleRepository);
        }

        #region CreateRoleAsync Tests

        [Test]
        public async Task CreateRoleAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var command = new CreateRoleCommand
            {
                Name = "Admin",
                Description = "Administrator role",
                IsSystemRole = false,
                CreatedBy = "admin-123"
            };

            _roleRepository.RoleExistsAsync(command.Name).Returns(Task.FromResult(false));
            _roleRepository.Add(Arg.Any<Role>()).Returns(callInfo => callInfo.Arg<Role>());

            // Act
            var result = await _roleService.CreateRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Name, Is.EqualTo(command.Name));
            Assert.That(result.Data.Description, Is.EqualTo(command.Description));
            _roleRepository.Received(1).Add(Arg.Any<Role>());
        }

        [Test]
        public async Task CreateRoleAsync_WithExistingRoleName_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateRoleCommand
            {
                Name = "Admin",
                Description = "Administrator role",
                CreatedBy = "admin-123"
            };

            _roleRepository.RoleExistsAsync(command.Name).Returns(Task.FromResult(true));

            // Act
            var result = await _roleService.CreateRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_EXISTS"));
            _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
        }

        [Test]
        public async Task CreateRoleAsync_WithEmptyName_ShouldReturnValidationFailure()
        {
            // Arrange
            var command = new CreateRoleCommand
            {
                Name = "",
                Description = "Administrator role",
                CreatedBy = "admin-123"
            };

            // Act
            var result = await _roleService.CreateRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Exists(e => e.PropertyName == "Name"), Is.True);
        }

        #endregion

        #region GetRoleByIdAsync Tests

        [Test]
        public async Task GetRoleByIdAsync_WithValidId_ShouldReturnRole()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role
            {
                Id = roleId,
                Name = "Admin",
                Description = "Administrator role"
            };

            _roleRepository.GetById(roleId).Returns(role);

            var query = new GetRoleByIdQuery { RoleId = roleId };

            // Act
            var result = await _roleService.GetRoleByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(roleId));
        }

        [Test]
        public async Task GetRoleByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var roleId = "non-existent";
            _roleRepository.GetById(roleId).Returns((Role?)null);

            var query = new GetRoleByIdQuery { RoleId = roleId };

            // Act
            var result = await _roleService.GetRoleByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_NOT_FOUND"));
        }

        #endregion

        #region GetRoleByNameAsync Tests

        [Test]
        public async Task GetRoleByNameAsync_WithValidName_ShouldReturnRole()
        {
            // Arrange
            var roleName = "Admin";
            var role = new Role
            {
                Id = "role-123",
                Name = roleName,
                Description = "Administrator role"
            };

            _roleRepository.GetByNameAsync(roleName).Returns(Task.FromResult<Role?>(role));

            var query = new GetRoleByNameQuery { Name = roleName };

            // Act
            var result = await _roleService.GetRoleByNameAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Name, Is.EqualTo(roleName));
        }

        #endregion

        #region GetAllRolesAsync Tests

        [Test]
        public async Task GetAllRolesAsync_ShouldReturnAllRoles()
        {
            // Arrange
            var roles = new List<Role>
            {
                new Role { Id = "1", Name = "Admin", Description = "Administrator" },
                new Role { Id = "2", Name = "User", Description = "Regular User" }
            };

            _roleRepository.GetAllRolesAsync().Returns(Task.FromResult(roles));

            // Act
            var result = await _roleService.GetAllRolesAsync();

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(2));
        }

        #endregion

        #region UpdateRoleAsync Tests

        [Test]
        public async Task UpdateRoleAsync_WithValidCommand_ShouldUpdateRole()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role
            {
                Id = roleId,
                Name = "Old Name",
                Description = "Old Description",
                IsSystemRole = false
            };

            _roleRepository.GetById(roleId).Returns(role);

            var command = new UpdateRoleCommand
            {
                RoleId = roleId,
                Name = "New Name",
                Description = "New Description",
                UpdatedBy = "admin-123"
            };

            _roleRepository.GetByNameAsync(command.Name).Returns(Task.FromResult<Role?>(null));

            // Act
            var result = await _roleService.UpdateRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.Name, Is.EqualTo("New Name"));
            Assert.That(result.Data.Description, Is.EqualTo("New Description"));
            _roleRepository.Received(1).Update(Arg.Any<Role>());
        }

        [Test]
        public async Task UpdateRoleAsync_WithSystemRole_ShouldReturnFailure()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role
            {
                Id = roleId,
                Name = "System Admin",
                Description = "System Administrator",
                IsSystemRole = true
            };

            _roleRepository.GetById(roleId).Returns(role);

            var command = new UpdateRoleCommand
            {
                RoleId = roleId,
                Name = "New Name",
                UpdatedBy = "admin-123"
            };

            // Act
            var result = await _roleService.UpdateRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SYSTEM_ROLE"));
            _roleRepository.DidNotReceive().Update(Arg.Any<Role>());
        }

        #endregion

        #region DeleteRoleAsync Tests

        [Test]
        public async Task DeleteRoleAsync_WithValidCommand_ShouldSoftDeleteRole()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role
            {
                Id = roleId,
                Name = "User",
                Description = "Regular User",
                IsSystemRole = false
            };

            _roleRepository.GetById(roleId).Returns(role);

            var command = new DeleteRoleCommand
            {
                RoleId = roleId,
                DeletedBy = "admin-123"
            };

            // Act
            var result = await _roleService.DeleteRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(role.DeletedBy, Is.EqualTo("admin-123"));
            _roleRepository.Received(1).Delete(Arg.Any<Role>());
        }

        [Test]
        public async Task DeleteRoleAsync_WithSystemRole_ShouldReturnFailure()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role
            {
                Id = roleId,
                Name = "System Admin",
                IsSystemRole = true
            };

            _roleRepository.GetById(roleId).Returns(role);

            var command = new DeleteRoleCommand
            {
                RoleId = roleId,
                DeletedBy = "admin-123"
            };

            // Act
            var result = await _roleService.DeleteRoleAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SYSTEM_ROLE"));
            _roleRepository.DidNotReceive().Delete(Arg.Any<Role>());
        }

        #endregion
    }
}
