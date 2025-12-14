# User Management Implementation Plan

## Overview
This document outlines the implementation plan for User Management features following Clean Architecture principles as defined in [001-CleanArchitectureStandard.md](001-CleanArchitectureStandard.md). The implementation will span both AppCore and AppInfra layers.

## Architecture Principles Applied

1. **Dependency Rule**: Core defines interfaces, Infrastructure implements them
2. **Core Independence**: AppCore has no dependencies on Infrastructure or external frameworks
3. **Service Pattern**: Business logic encapsulated in Service classes with command/query pattern
4. **Result Pattern**: All service methods return AppResult objects for consistent error handling
5. **Repository Pattern**: Data access through IRepository interface

---

## AppCore Layer Implementation

### 1. Entities (Domain Models)

All entities inherit from `BaseEntity.cs` which provides:
- `Id` (string)
- `CreatedAt`, `CreatedBy`
- `UpdatedAt`, `UpdatedBy`
- `DeletedAt`, `DeletedBy`
- `IsDeleted` (soft delete support)

#### User Entity
**File**: `AppCore/Entities/User.cs`

```csharp
public class User : BaseEntity
{
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string FirebaseUid { get; set; }  // Firebase Auth UID
    public bool EmailVerified { get; set; }
    public string ProfilePhotoUrl { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public List<string> Skills { get; set; }
    public List<string> Interests { get; set; }
    public List<string> AreasOfExpertise { get; set; }
    public Dictionary<string, string> SocialLinks { get; set; }  // LinkedIn, GitHub, Twitter, etc.
    public UserPrivacySettings PrivacySettings { get; set; }
    public UserStatus Status { get; set; }  // Active, Disabled, Suspended
    public DateTime? LastLoginAt { get; set; }
}

public enum UserStatus
{
    Active,
    Disabled,
    Suspended,
    PendingVerification
}
```

#### UserPrivacySettings (Value Object)
**File**: `AppCore/Entities/UserPrivacySettings.cs`

```csharp
public class UserPrivacySettings
{
    public bool ProfileVisible { get; set; }
    public bool ShowEmail { get; set; }
    public bool ShowLocation { get; set; }
    public bool AllowFollowers { get; set; }
    
    public static UserPrivacySettings GetDefault()
    {
        return new UserPrivacySettings
        {
            ProfileVisible = true,
            ShowEmail = false,
            ShowLocation = true,
            AllowFollowers = true
        };
    }
}
```

---

### 2. Interfaces

#### IUserRepository
**File**: `AppCore/Interfaces/IUserRepository.cs`

Extends `IRepository<User>` with additional user-specific queries:

```csharp
public interface IUserRepository : IRepository<User>
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByFirebaseUidAsync(string firebaseUid);
    Task<List<User>> SearchUsersAsync(string searchTerm);
    Task<List<User>> GetUsersByStatusAsync(UserStatus status);
    Task<bool> EmailExistsAsync(string email);
}
```

#### IAuthenticationService
**File**: `AppCore/Interfaces/IAuthenticationService.cs`

Interface for Firebase Auth integration (implemented in AppInfra):

```csharp
public interface IAuthenticationService
{
    Task<AuthResult> VerifyFirebaseTokenAsync(string idToken);
    Task<AuthResult> CreateFirebaseUserAsync(string email, string password);
    Task<AuthResult> UpdateFirebaseUserAsync(string uid, UpdateUserRequest request);
    Task<bool> SendPasswordResetEmailAsync(string email);
    Task<bool> SendEmailVerificationAsync(string uid);
}

public class AuthResult
{
    public bool Success { get; set; }
    public string Uid { get; set; }
    public string Email { get; set; }
    public bool EmailVerified { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

### 3. DTOs (Data Transfer Objects)

#### Commands (State Modification)
**File**: `AppCore/DTOs/UserCommands.cs`

```csharp
public class RegisterUserCommand
{
    public string Email { get; set; }
    public string DisplayName { get; set; }
    // Note: Password is not stored as Firebase handles OAuth authentication
}

public class UpdateUserProfileCommand
{
    public string UserId { get; set; }
    public string DisplayName { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public string ProfilePhotoUrl { get; set; }
    public List<string> Skills { get; set; }
    public List<string> Interests { get; set; }
    public List<string> AreasOfExpertise { get; set; }
    public Dictionary<string, string> SocialLinks { get; set; }
}

public class UpdatePrivacySettingsCommand
{
    public string UserId { get; set; }
    public UserPrivacySettings PrivacySettings { get; set; }
}

public class DisableUserCommand
{
    public string UserId { get; set; }
    public string Reason { get; set; }
    public string DisabledBy { get; set; }
}
```

#### Queries (Data Retrieval)
**File**: `AppCore/DTOs/UserQueries.cs`

```csharp
public class GetUserByIdQuery
{
    public string UserId { get; set; }
}

public class GetUserByEmailQuery
{
    public string Email { get; set; }
}

public class SearchUsersQuery
{
    public string SearchTerm { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
}

public class GetUserActivityHistoryQuery
{
    public string UserId { get; set; }
}
```

---

### 4. AppResult (Result Pattern)

**File**: `AppCore/Common/AppResult.cs`

```csharp
public class AppResult<T>
{
    public bool Success { get; set; }
    public T Data { get; set; }
    public string Message { get; set; }
    public List<ValidationError> ValidationErrors { get; set; }
    public string ErrorCode { get; set; }

    public static AppResult<T> SuccessResult(T data, string message = null)
    {
        return new AppResult<T>
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public static AppResult<T> FailureResult(string message, string errorCode = null)
    {
        return new AppResult<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            ValidationErrors = new List<ValidationError>()
        };
    }

    public static AppResult<T> ValidationFailure(List<ValidationError> errors)
    {
        return new AppResult<T>
        {
            Success = false,
            Message = "Validation failed",
            ValidationErrors = errors
        };
    }
}

public class ValidationError
{
    public string PropertyName { get; set; }
    public string ErrorMessage { get; set; }
}
```

---

### 5. Service Classes (Business Logic)

#### UserService
**File**: `AppCore/Services/UserService.cs`

Contains all business logic for user management:

```csharp
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

    // Additional methods...
    
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
```

---

## AppInfra Layer Implementation

### 1. Repository Implementations

#### UserRepository
**File**: `AppInfra/Repositories/UserRepository.cs`

```csharp
public class UserRepository : IUserRepository
{
    private readonly DbContext _dbContext;  // or whatever data access you're using

    public UserRepository(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public User GetById(string id)
    {
        return _dbContext.Users.FirstOrDefault(u => u.Id == id && !u.IsDeleted);
    }

    public User Add(User entity)
    {
        _dbContext.Users.Add(entity);
        _dbContext.SaveChanges();
        return entity;
    }

    public void Update(User entity)
    {
        _dbContext.Users.Update(entity);
        _dbContext.SaveChanges();
    }

    public void Delete(User entity)
    {
        // Soft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        Update(entity);
    }

    public bool RecordExists(string id)
    {
        return _dbContext.Users.Any(u => u.Id == id && !u.IsDeleted);
    }

    public async Task<User> GetByEmailAsync(string email)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted);
    }

    public async Task<User> GetByFirebaseUidAsync(string firebaseUid)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid && !u.IsDeleted);
    }

    public async Task<List<User>> SearchUsersAsync(string searchTerm)
    {
        return await _dbContext.Users
            .Where(u => !u.IsDeleted && 
                   (u.DisplayName.Contains(searchTerm) || 
                    u.Email.Contains(searchTerm)))
            .ToListAsync();
    }

    public async Task<List<User>> GetUsersByStatusAsync(UserStatus status)
    {
        return await _dbContext.Users
            .Where(u => u.Status == status && !u.IsDeleted)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbContext.Users
            .AnyAsync(u => u.Email == email && !u.IsDeleted);
    }
}
```

---

### 2. External Service Implementations

#### FirebaseAuthenticationService
**File**: `AppInfra/Services/FirebaseAuthenticationService.cs`

```csharp
public class FirebaseAuthenticationService : IAuthenticationService
{
    private readonly FirebaseAuth _firebaseAuth;

    public FirebaseAuthenticationService(IConfiguration configuration)
    {
        // Initialize Firebase Admin SDK
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(configuration["Firebase:ServiceAccountKeyPath"])
        });
        _firebaseAuth = FirebaseAuth.DefaultInstance;
    }

    public async Task<AuthResult> VerifyFirebaseTokenAsync(string idToken)
    {
        try
        {
            var decodedToken = await _firebaseAuth.VerifyIdTokenAsync(idToken);
            return new AuthResult
            {
                Success = true,
                Uid = decodedToken.Uid,
                Email = decodedToken.Claims["email"]?.ToString(),
                EmailVerified = (bool)(decodedToken.Claims["email_verified"] ?? false)
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<AuthResult> CreateFirebaseUserAsync(string email, string password)
    {
        try
        {
            var userRecordArgs = new UserRecordArgs
            {
                Email = email,
                Password = password,
                EmailVerified = false,
                Disabled = false
            };

            var userRecord = await _firebaseAuth.CreateUserAsync(userRecordArgs);
            
            return new AuthResult
            {
                Success = true,
                Uid = userRecord.Uid,
                Email = userRecord.Email,
                EmailVerified = userRecord.EmailVerified
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<bool> SendPasswordResetEmailAsync(string email)
    {
        try
        {
            var link = await _firebaseAuth.GeneratePasswordResetLinkAsync(email);
            // Send email with link (implement email service separately)
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> SendEmailVerificationAsync(string uid)
    {
        try
        {
            var link = await _firebaseAuth.GenerateEmailVerificationLinkAsync(await GetEmailFromUid(uid));
            // Send email with link (implement email service separately)
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetEmailFromUid(string uid)
    {
        var userRecord = await _firebaseAuth.GetUserAsync(uid);
        return userRecord.Email;
    }

    public async Task<AuthResult> UpdateFirebaseUserAsync(string uid, UpdateUserRequest request)
    {
        try
        {
            var args = new UserRecordArgs
            {
                Uid = uid,
                Email = request.Email,
                DisplayName = request.DisplayName
            };

            var userRecord = await _firebaseAuth.UpdateUserAsync(args);
            
            return new AuthResult
            {
                Success = true,
                Uid = userRecord.Uid,
                Email = userRecord.Email
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}
```

---

## Implementation Sequence

### Phase 1: Core Foundation
1. ✅ Create `AppResult<T>` class
2. ✅ Create `ValidationError` class
3. ✅ Create `User` entity
4. ✅ Create `UserPrivacySettings` value object
5. ✅ Create DTOs (Commands and Queries)

### Phase 2: Interfaces
6. ✅ Define `IUserRepository` interface
7. ✅ Define `IAuthenticationService` interface
8. ✅ Define `IUserService` interface

### Phase 3: Business Logic
9. ✅ Implement `UserService` class with all business logic
10. ✅ Add validation methods
11. ✅ Write unit tests for `UserService`

### Phase 4: Infrastructure
12. ✅ Implement `UserRepository` (data access)
13. ✅ Implement `FirebaseAuthenticationService`
14. ✅ Configure dependency injection in `Program.cs`

### Phase 5: API Layer
15. ✅ Create `UsersController` in IteraWebApi
16. ✅ Map endpoints to service calls
17. ✅ Add JWT middleware for authentication
18. ✅ Add API documentation (Swagger)

### Phase 6: Testing & Refinement
19. ✅ Integration tests
20. ✅ End-to-end testing
21. ✅ Security audit

---

## Dependency Injection Setup

**File**: `IteraWebApi/Program.cs`

```csharp
// AppCore services
builder.Services.AddScoped<IUserService, UserService>();

// AppInfra repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();

// AppInfra external services
builder.Services.AddSingleton<IAuthenticationService, FirebaseAuthenticationService>();

// Database context
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

---

## Testing Strategy

### Unit Tests (AppCore.UnitTests)
- Test `UserService` methods with mocked dependencies
- Test validation logic
- Test business rules (e.g., email uniqueness, status transitions)

### Integration Tests
- Test repository implementations with in-memory database
- Test Firebase Auth integration with test accounts

---

## Summary

This implementation plan follows Clean Architecture principles:
- ✅ **Separation of Concerns**: Domain logic in AppCore, infrastructure in AppInfra
- ✅ **Dependency Inversion**: Core defines interfaces, Infrastructure implements
- ✅ **Command/Query Pattern**: All service methods use DTOs as input
- ✅ **Result Pattern**: Consistent error handling with AppResult<T>
- ✅ **Testability**: Business logic isolated and easily testable
- ✅ **Framework Independence**: Core has no dependencies on EF, Firebase, or ASP.NET

The implementation provides:
1. Complete user registration with Firebase Auth integration
2. Profile management with privacy settings
3. Administrative user management (disable/enable accounts)
4. User search functionality
5. Extensible architecture for future features (followers, activity tracking, etc.)
