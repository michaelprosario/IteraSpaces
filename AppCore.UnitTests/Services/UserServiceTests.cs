using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using NSubstitute;
using NUnit.Framework;
using Assert = NUnit.Framework.Assert;

namespace AppCore.UnitTests.Services
{
    [TestFixture]
    public class UserServiceTests
    {
        private IUserRepository _userRepository;
        private IAuthenticationService _authService;
        private UserService _userService;

        [SetUp]
        public void Setup()
        {
            _userRepository = Substitute.For<IUserRepository>();
            _authService = Substitute.For<IAuthenticationService>();
            _userService = new UserService(_userRepository, _authService);
        }

        #region RegisterUserAsync Tests

        [Test]
        public async Task RegisterUserAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "password123",
                DisplayName = "Test User"
            };

            _userRepository.EmailExistsAsync(command.Email).Returns(Task.FromResult(false));

            _authService.CreateFirebaseUserAsync(command.Email, command.Password)
                .Returns(Task.FromResult(new AuthResult
                {
                    Success = true,
                    Uid = "firebase-uid-123",
                    Email = command.Email,
                    EmailVerified = false
                }));

            _userRepository.Add(Arg.Any<User>()).Returns(callInfo => callInfo.Arg<User>());

            _authService.SendEmailVerificationAsync(Arg.Any<string>()).Returns(Task.FromResult(true));

            // Act
            var result = await _userService.RegisterUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Email, Is.EqualTo(command.Email));
            Assert.That(result.Data.DisplayName, Is.EqualTo(command.DisplayName));
            Assert.That(result.Data.Status, Is.EqualTo(UserStatus.PendingVerification));
            _userRepository.Received(1).Add(Arg.Any<User>());
        }

        [Test]
        public async Task RegisterUserAsync_WithExistingEmail_ShouldReturnFailure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "existing@example.com",
                Password = "password123",
                DisplayName = "Test User"
            };

            _userRepository.EmailExistsAsync(command.Email).Returns(Task.FromResult(true));

            // Act
            var result = await _userService.RegisterUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EMAIL_EXISTS"));
            _userRepository.DidNotReceive().Add(Arg.Any<User>());
        }

        [Test]
        public async Task RegisterUserAsync_WithInvalidEmail_ShouldReturnValidationFailure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "",
                Password = "password123",
                DisplayName = "Test User"
            };

            // Act
            var result = await _userService.RegisterUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Exists(e => e.PropertyName == "Email"), Is.True);
        }

        [Test]
        public async Task RegisterUserAsync_WithShortPassword_ShouldReturnValidationFailure()
        {
            // Arrange
            var command = new RegisterUserCommand
            {
                Email = "test@example.com",
                Password = "short",
                DisplayName = "Test User"
            };

            // Act
            var result = await _userService.RegisterUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Exists(e => e.PropertyName == "Password"), Is.True);
        }

        #endregion

        #region GetUserByIdAsync Tests

        [Test]
        public async Task GetUserByIdAsync_WithValidId_ShouldReturnUser()
        {
            // Arrange
            var userId = "user-123";
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Test User"
            };

            _userRepository.GetById(userId).Returns(user);

            var query = new GetUserByIdQuery { UserId = userId };

            // Act
            var result = await _userService.GetUserByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(userId));
        }

        [Test]
        public async Task GetUserByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var userId = "non-existent";
            _userRepository.GetById(userId).Returns((User)null);

            var query = new GetUserByIdQuery { UserId = userId };

            // Act
            var result = await _userService.GetUserByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("USER_NOT_FOUND"));
        }

        #endregion

        #region UpdateUserProfileAsync Tests

        [Test]
        public async Task UpdateUserProfileAsync_WithValidCommand_ShouldUpdateProfile()
        {
            // Arrange
            var userId = "user-123";
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                DisplayName = "Old Name",
                Bio = "Old Bio"
            };

            _userRepository.GetById(userId).Returns(user);

            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                DisplayName = "New Name",
                Bio = "New Bio"
            };

            // Act
            var result = await _userService.UpdateUserProfileAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data.DisplayName, Is.EqualTo("New Name"));
            Assert.That(result.Data.Bio, Is.EqualTo("New Bio"));
            _userRepository.Received(1).Update(Arg.Any<User>());
        }

        [Test]
        public async Task UpdateUserProfileAsync_WithNonExistentUser_ShouldReturnFailure()
        {
            // Arrange
            var userId = "non-existent";
            _userRepository.GetById(userId).Returns((User)null);

            var command = new UpdateUserProfileCommand
            {
                UserId = userId,
                DisplayName = "New Name"
            };

            // Act
            var result = await _userService.UpdateUserProfileAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("USER_NOT_FOUND"));
        }

        #endregion

        #region DisableUserAsync Tests

        [Test]
        public async Task DisableUserAsync_WithValidCommand_ShouldDisableUser()
        {
            // Arrange
            var userId = "user-123";
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                Status = UserStatus.Active
            };

            _userRepository.GetById(userId).Returns(user);

            var command = new DisableUserCommand
            {
                UserId = userId,
                Reason = "Policy violation",
                DisabledBy = "admin-123"
            };

            // Act
            var result = await _userService.DisableUserAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(user.Status, Is.EqualTo(UserStatus.Disabled));
            _userRepository.Received(1).Update(Arg.Any<User>());
        }

        #endregion

        #region SearchUsersAsync Tests

        [Test]
        public async Task SearchUsersAsync_WithValidQuery_ShouldReturnUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = "1", Email = "user1@example.com", DisplayName = "User One" },
                new User { Id = "2", Email = "user2@example.com", DisplayName = "User Two" }
            };

            _userRepository.SearchUsersAsync("User").Returns(Task.FromResult(users));

            var query = new SearchUsersQuery
            {
                SearchTerm = "User",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _userService.SearchUsersAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Count, Is.EqualTo(2));
        }

        [Test]
        public async Task SearchUsersAsync_WithEmptySearchTerm_ShouldReturnFailure()
        {
            // Arrange
            var query = new SearchUsersQuery
            {
                SearchTerm = "",
                PageNumber = 1,
                PageSize = 10
            };

            // Act
            var result = await _userService.SearchUsersAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_INPUT"));
        }

        #endregion

        #region RecordLoginAsync Tests

        [Test]
        public async Task RecordLoginAsync_WithValidUserId_ShouldUpdateLastLogin()
        {
            // Arrange
            var userId = "user-123";
            var user = new User
            {
                Id = userId,
                Email = "test@example.com",
                LastLoginAt = null
            };

            _userRepository.GetById(userId).Returns(user);

            // Act
            var result = await _userService.RecordLoginAsync(userId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(user.LastLoginAt, Is.Not.Null);
            _userRepository.Received(1).Update(Arg.Any<User>());
        }

        #endregion
    }
}
