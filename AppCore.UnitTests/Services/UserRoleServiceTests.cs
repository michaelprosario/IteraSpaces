using System;
using System.Collections.Generic;
using System.Linq;
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
    public class UserRoleServiceTests
    {
        private IUserRoleRepository _userRoleRepository;
        private IUserRepository _userRepository;
        private IRoleRepository _roleRepository;
        private UserRoleService _userRoleService;

        [SetUp]
        public void Setup()
        {
            _userRoleRepository = Substitute.For<IUserRoleRepository>();
            _userRepository = Substitute.For<IUserRepository>();
            _roleRepository = Substitute.For<IRoleRepository>();
            _userRoleService = new UserRoleService(_userRoleRepository, _userRepository, _roleRepository);
        }

        #region AssignRoleToUserAsync Tests

        [Test]
        public async Task AssignRoleToUserAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            var role = new Role
            {
                Id = roleId,
                Name = "Admin",
                Description = "Administrator role"
            };

            _userRepository.GetById(userId).Returns(user);
            _roleRepository.GetById(roleId).Returns(role);
            _userRoleRepository.UserHasRoleAsync(userId, roleId).Returns(Task.FromResult(false));
            _userRoleRepository.Add(Arg.Any<UserRole>()).Returns(callInfo => callInfo.Arg<UserRole>());

            var command = new AssignRoleToUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.AssignRoleToUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.UserId, Is.EqualTo(userId));
            Assert.That(result.Data.RoleId, Is.EqualTo(roleId));
            _userRoleRepository.Received(1).Add(Arg.Any<UserRole>());
        }

        [Test]
        public async Task AssignRoleToUserAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = "non-existent";
            var roleId = "role-123";

            _userRepository.GetById(userId).Returns((User?)null);

            var command = new AssignRoleToUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.AssignRoleToUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("USER_NOT_FOUND"));
            _userRoleRepository.DidNotReceive().Add(Arg.Any<UserRole>());
        }

        [Test]
        public async Task AssignRoleToUserAsync_WithNonExistentRole_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "non-existent";

            var user = new User { Id = userId, Email = "test@example.com" };

            _userRepository.GetById(userId).Returns(user);
            _roleRepository.GetById(roleId).Returns((Role?)null);

            var command = new AssignRoleToUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.AssignRoleToUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_NOT_FOUND"));
            _userRoleRepository.DidNotReceive().Add(Arg.Any<UserRole>());
        }

        [Test]
        public async Task AssignRoleToUserAsync_WhenUserAlreadyHasRole_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            var user = new User { Id = userId, Email = "test@example.com" };
            var role = new Role { Id = roleId, Name = "Admin" };

            _userRepository.GetById(userId).Returns(user);
            _roleRepository.GetById(roleId).Returns(role);
            _userRoleRepository.UserHasRoleAsync(userId, roleId).Returns(Task.FromResult(true));

            var command = new AssignRoleToUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.AssignRoleToUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_ALREADY_ASSIGNED"));
            _userRoleRepository.DidNotReceive().Add(Arg.Any<UserRole>());
        }

        #endregion

        #region RemoveRoleFromUserAsync Tests

        [Test]
        public async Task RemoveRoleFromUserAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            var userRole = new UserRole
            {
                Id = "ur-123",
                UserId = userId,
                RoleId = roleId
            };

            _userRoleRepository.GetUserRoleAsync(userId, roleId).Returns(Task.FromResult<UserRole?>(userRole));

            var command = new RemoveRoleFromUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                RemovedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.RemoveRoleFromUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(userRole.DeletedBy, Is.EqualTo("admin-123"));
            _userRoleRepository.Received(1).Delete(Arg.Any<UserRole>());
        }

        [Test]
        public async Task RemoveRoleFromUserAsync_WhenUserDoesNotHaveRole_ShouldReturnFailure()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            _userRoleRepository.GetUserRoleAsync(userId, roleId).Returns(Task.FromResult<UserRole?>(null));

            var command = new RemoveRoleFromUserCommand
            {
                UserId = userId,
                RoleId = roleId,
                RemovedBy = "admin-123"
            };

            // Act
            var result = await _userRoleService.RemoveRoleFromUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_NOT_ASSIGNED"));
            _userRoleRepository.DidNotReceive().Delete(Arg.Any<UserRole>());
        }

        #endregion

        #region GetUserRolesAsync Tests

        [Test]
        public async Task GetUserRolesAsync_WithValidUserId_ShouldReturnUserRoles()
        {
            // Arrange
            var userId = "user-123";
            var user = new User { Id = userId, Email = "test@example.com" };

            var userRoles = new List<UserRole>
            {
                new UserRole
                {
                    Id = "ur-1",
                    UserId = userId,
                    RoleId = "role-1",
                    Role = new Role { Id = "role-1", Name = "Admin", Description = "Administrator" }
                },
                new UserRole
                {
                    Id = "ur-2",
                    UserId = userId,
                    RoleId = "role-2",
                    Role = new Role { Id = "role-2", Name = "User", Description = "Regular User" }
                }
            };

            _userRepository.GetById(userId).Returns(user);
            _userRoleRepository.GetUserRolesAsync(userId).Returns(Task.FromResult(userRoles));

            var query = new GetUserRolesQuery { UserId = userId };

            // Act
            var result = await _userRoleService.GetUserRolesAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(2));
            Assert.That(result.Data.Any(r => r.Name == "Admin"), Is.True);
        }

        [Test]
        public async Task GetUserRolesAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = "non-existent";
            _userRepository.GetById(userId).Returns((User?)null);

            var query = new GetUserRolesQuery { UserId = userId };

            // Act
            var result = await _userRoleService.GetUserRolesAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("USER_NOT_FOUND"));
        }

        #endregion

        #region GetUserRoleNamesAsync Tests

        [Test]
        public async Task GetUserRoleNamesAsync_WithValidUserId_ShouldReturnRoleNames()
        {
            // Arrange
            var userId = "user-123";
            var user = new User { Id = userId, Email = "test@example.com" };

            var roleNames = new List<string> { "Admin", "User" };

            _userRepository.GetById(userId).Returns(user);
            _userRoleRepository.GetUserRoleNamesAsync(userId).Returns(Task.FromResult(roleNames));

            var query = new GetUserRolesQuery { UserId = userId };

            // Act
            var result = await _userRoleService.GetUserRoleNamesAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(2));
            Assert.That(result.Data.Contains("Admin"), Is.True);
        }

        #endregion

        #region UserHasRoleAsync Tests

        [Test]
        public async Task UserHasRoleAsync_WhenUserHasRole_ShouldReturnTrue()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            _userRoleRepository.UserHasRoleAsync(userId, roleId).Returns(Task.FromResult(true));

            // Act
            var result = await _userRoleService.UserHasRoleAsync(userId, roleId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.True);
        }

        [Test]
        public async Task UserHasRoleAsync_WhenUserDoesNotHaveRole_ShouldReturnFalse()
        {
            // Arrange
            var userId = "user-123";
            var roleId = "role-123";

            _userRoleRepository.UserHasRoleAsync(userId, roleId).Returns(Task.FromResult(false));

            // Act
            var result = await _userRoleService.UserHasRoleAsync(userId, roleId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.False);
        }

        #endregion

        #region GetUsersInRoleAsync Tests

        [Test]
        public async Task GetUsersInRoleAsync_WithValidRoleId_ShouldReturnUsers()
        {
            // Arrange
            var roleId = "role-123";
            var role = new Role { Id = roleId, Name = "Admin" };

            var users = new List<User>
            {
                new User { Id = "user-1", Email = "user1@example.com", DisplayName = "User One" },
                new User { Id = "user-2", Email = "user2@example.com", DisplayName = "User Two" }
            };

            _roleRepository.GetById(roleId).Returns(role);
            _userRoleRepository.GetUsersInRoleAsync(roleId).Returns(Task.FromResult(users));

            var query = new GetUsersInRoleQuery { RoleId = roleId };

            // Act
            var result = await _userRoleService.GetUsersInRoleAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task GetUsersInRoleAsync_WithNonExistentRole_ShouldReturnFailure()
        {
            // Arrange
            var roleId = "non-existent";
            _roleRepository.GetById(roleId).Returns((Role?)null);

            var query = new GetUsersInRoleQuery { RoleId = roleId };

            // Act
            var result = await _userRoleService.GetUsersInRoleAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ROLE_NOT_FOUND"));
        }

        #endregion
    }
}
