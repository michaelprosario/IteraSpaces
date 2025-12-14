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
    public interface IUserService
    {
        Task<AppResult<User>> RegisterUserAsync(RegisterUserCommand command);
        Task<AppResult<User>> GetUserByIdAsync(GetUserByIdQuery query);
        Task<AppResult<User>> GetUserByEmailAsync(GetUserByEmailQuery query);
        Task<AppResult<User>> UpdateUserProfileAsync(UpdateUserProfileCommand command);
        Task<AppResult<User>> UpdatePrivacySettingsAsync(UpdatePrivacySettingsCommand command);
        Task<AppResult<bool>> DisableUserAsync(DisableUserCommand command);
        Task<AppResult<bool>> EnableUserAsync(string userId, string enabledBy);
        Task<AppResult<List<User>>> SearchUsersAsync(SearchUsersQuery query);
        Task<AppResult<bool>> RecordLoginAsync(string userId);
    }

    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IAuthenticationService _authService;

        public UserService(IUserRepository userRepository, IAuthenticationService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        public async Task<AppResult<User>> RegisterUserAsync(RegisterUserCommand command)
        {
            // 1. Validation
            var validationErrors = ValidateRegistration(command);
            if (validationErrors.Any())
                return AppResult<User>.ValidationFailure(validationErrors);

            // 2. Check if email already exists
            if (await _userRepository.EmailExistsAsync(command.Email))
                return AppResult<User>.FailureResult("Email already registered", "EMAIL_EXISTS");

            // 3. Create Firebase user
            var authResult = await _authService.CreateFirebaseUserAsync(command.Email, command.Password);
            if (!authResult.Success)
                return AppResult<User>.FailureResult(authResult.ErrorMessage, "AUTH_FAILED");

            // 4. Create user entity
            var user = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = command.Email,
                DisplayName = command.DisplayName,
                FirebaseUid = authResult.Uid,
                EmailVerified = false,
                Status = UserStatus.PendingVerification,
                PrivacySettings = UserPrivacySettings.GetDefault(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "SYSTEM",
                Skills = new List<string>(),
                Interests = new List<string>(),
                AreasOfExpertise = new List<string>(),
                SocialLinks = new Dictionary<string, string>()
            };

            // 5. Save to repository
            var savedUser = _userRepository.Add(user);

            // 6. Send verification email
            await _authService.SendEmailVerificationAsync(authResult.Uid);

            return AppResult<User>.SuccessResult(savedUser, "User registered successfully");
        }

        public async Task<AppResult<User>> GetUserByIdAsync(GetUserByIdQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.UserId))
                return AppResult<User>.FailureResult("User ID is required", "INVALID_INPUT");

            var user = _userRepository.GetById(query.UserId);
            if (user == null)
                return AppResult<User>.FailureResult("User not found", "USER_NOT_FOUND");

            return AppResult<User>.SuccessResult(user);
        }

        public async Task<AppResult<User>> GetUserByEmailAsync(GetUserByEmailQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.Email))
                return AppResult<User>.FailureResult("Email is required", "INVALID_INPUT");

            var user = await _userRepository.GetByEmailAsync(query.Email);
            if (user == null)
                return AppResult<User>.FailureResult("User not found", "USER_NOT_FOUND");

            return AppResult<User>.SuccessResult(user);
        }

        public async Task<AppResult<User>> UpdateUserProfileAsync(UpdateUserProfileCommand command)
        {
            // 1. Validation
            var validationErrors = ValidateProfileUpdate(command);
            if (validationErrors.Any())
                return AppResult<User>.ValidationFailure(validationErrors);

            // 2. Get existing user
            var user = _userRepository.GetById(command.UserId);
            if (user == null)
                return AppResult<User>.FailureResult("User not found", "USER_NOT_FOUND");

            // 3. Update properties
            user.DisplayName = command.DisplayName ?? user.DisplayName;
            user.Bio = command.Bio ?? user.Bio;
            user.Location = command.Location ?? user.Location;
            user.ProfilePhotoUrl = command.ProfilePhotoUrl ?? user.ProfilePhotoUrl;
            user.Skills = command.Skills ?? user.Skills;
            user.Interests = command.Interests ?? user.Interests;
            user.AreasOfExpertise = command.AreasOfExpertise ?? user.AreasOfExpertise;
            user.SocialLinks = command.SocialLinks ?? user.SocialLinks;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = command.UserId;

            // 4. Save changes
            _userRepository.Update(user);

            return AppResult<User>.SuccessResult(user, "Profile updated successfully");
        }

        public async Task<AppResult<User>> UpdatePrivacySettingsAsync(UpdatePrivacySettingsCommand command)
        {
            if (string.IsNullOrWhiteSpace(command.UserId))
                return AppResult<User>.FailureResult("User ID is required", "INVALID_INPUT");

            var user = _userRepository.GetById(command.UserId);
            if (user == null)
                return AppResult<User>.FailureResult("User not found", "USER_NOT_FOUND");

            user.PrivacySettings = command.PrivacySettings;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = command.UserId;

            _userRepository.Update(user);

            return AppResult<User>.SuccessResult(user, "Privacy settings updated successfully");
        }

        public async Task<AppResult<bool>> DisableUserAsync(DisableUserCommand command)
        {
            var user = _userRepository.GetById(command.UserId);
            if (user == null)
                return AppResult<bool>.FailureResult("User not found", "USER_NOT_FOUND");

            user.Status = UserStatus.Disabled;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = command.DisabledBy;

            _userRepository.Update(user);

            return AppResult<bool>.SuccessResult(true, $"User disabled: {command.Reason}");
        }

        public async Task<AppResult<bool>> EnableUserAsync(string userId, string enabledBy)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
                return AppResult<bool>.FailureResult("User not found", "USER_NOT_FOUND");

            user.Status = UserStatus.Active;
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = enabledBy;

            _userRepository.Update(user);

            return AppResult<bool>.SuccessResult(true, "User enabled successfully");
        }

        public async Task<AppResult<List<User>>> SearchUsersAsync(SearchUsersQuery query)
        {
            if (string.IsNullOrWhiteSpace(query.SearchTerm))
                return AppResult<List<User>>.FailureResult("Search term is required", "INVALID_INPUT");

            var users = await _userRepository.SearchUsersAsync(query.SearchTerm);
            
            // Apply pagination
            var paginatedUsers = users
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();

            return AppResult<List<User>>.SuccessResult(paginatedUsers, $"Found {users.Count} users");
        }

        public async Task<AppResult<bool>> RecordLoginAsync(string userId)
        {
            var user = _userRepository.GetById(userId);
            if (user == null)
                return AppResult<bool>.FailureResult("User not found", "USER_NOT_FOUND");

            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);

            return AppResult<bool>.SuccessResult(true, "Login recorded");
        }

        private List<ValidationError> ValidateRegistration(RegisterUserCommand command)
        {
            var errors = new List<ValidationError>();
            
            if (string.IsNullOrWhiteSpace(command.Email))
                errors.Add(new ValidationError { PropertyName = "Email", ErrorMessage = "Email is required" });
            
            if (string.IsNullOrWhiteSpace(command.Password))
                errors.Add(new ValidationError { PropertyName = "Password", ErrorMessage = "Password is required" });
            else if (command.Password.Length < 8)
                errors.Add(new ValidationError { PropertyName = "Password", ErrorMessage = "Password must be at least 8 characters" });
            
            if (string.IsNullOrWhiteSpace(command.DisplayName))
                errors.Add(new ValidationError { PropertyName = "DisplayName", ErrorMessage = "Display name is required" });
            
            return errors;
        }
        
        private List<ValidationError> ValidateProfileUpdate(UpdateUserProfileCommand command)
        {
            var errors = new List<ValidationError>();
            
            if (string.IsNullOrWhiteSpace(command.UserId))
                errors.Add(new ValidationError { PropertyName = "UserId", ErrorMessage = "User ID is required" });
            
            return errors;
        }
    }
}
