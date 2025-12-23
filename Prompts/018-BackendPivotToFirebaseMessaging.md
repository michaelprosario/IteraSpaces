# Backend Plan: Pivot from SignalR to Firebase Cloud Messaging

**Date**: December 22, 2025  
**Status**: Planning  
**Author**: Engineering Team

## Executive Summary

This document outlines the backend architectural changes required to migrate real-time communication for Lean Coffee features (sessions, topics, and participants) from SignalR to Firebase Cloud Messaging (FCM). This pivot leverages the existing Firebase infrastructure already in place for authentication.

**Scope**: This implementation focuses exclusively on **web browser clients** using Firebase Cloud Messaging for Web Push notifications. Mobile native apps (iOS/Android) are not in scope.

---

## Current State Analysis

### Existing SignalR Implementation

**Hub Location**: [IteraWebApi/Hubs/LeanSessionHub.cs](../IteraWebApi/Hubs/LeanSessionHub.cs)

**Current Real-time Events**:
1. **Connection Management**:
   - `JoinSession(sessionId, userId)` - User joins a session group
   - `LeaveSession(sessionId, userId)` - User leaves a session group
   - `OnDisconnectedAsync()` - Handle connection loss

2. **Event Notifications**:
   - `NotifyTopicAdded(sessionId, topicId)` - Broadcast new topic
   - `NotifyTopicUpdated(sessionId, topicId)` - Broadcast topic changes
   - `NotifyTopicStatusChanged(sessionId, topicId)` - Status transitions
   - `NotifyVoteCast(sessionId, topicId)` - Vote updates
   - `NotifyVoteRemoved(sessionId, topicId)` - Vote removals
   - `NotifySessionStateChanged(sessionId)` - Session status updates
   - `NotifyCurrentTopicChanged(sessionId, topicId)` - Active topic changes

**Controllers Using SignalR**:
- [LeanSessionsController.cs](../IteraWebApi/Controllers/LeanSessionsController.cs) - `IHubContext<LeanSessionHub>`
- [LeanTopicsController.cs](../IteraWebApi/Controllers/LeanTopicsController.cs) - `IHubContext<LeanSessionHub>`
- [LeanParticipantsController.cs](../IteraWebApi/Controllers/LeanParticipantsController.cs) - `IHubContext<LeanSessionHub>`

**SignalR Configuration**: [Program.cs](../IteraWebApi/Program.cs#L116-L122)

### Existing Firebase Infrastructure

**Firebase Admin SDK**: Already configured in [Program.cs](../IteraWebApi/Program.cs#L14-L31)
- Firebase Auth: Active for JWT authentication
- Firebase Project: Configured via `firebase-admin-sdk.json`
- Firebase credentials: Environment-based configuration

---

## Migration Strategy

### Phase 1: Add FCM Infrastructure (Backend)

#### 1.1 Install Required NuGet Package
Firebase Admin SDK already includes FCM capabilities - no additional packages needed.

#### 1.2 Create FCM Service Interface

**File**: `AppCore/Interfaces/IFirebaseMessagingService.cs`

```csharp
namespace AppCore.Interfaces;

public interface IFirebaseMessagingService
{
    /// <summary>
    /// Send a notification to a specific device token
    /// </summary>
    Task<string> SendToDeviceAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
    
    /// <summary>
    /// Send a notification to a topic (e.g., "session_abc123")
    /// </summary>
    Task<string> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null);
    
    /// <summary>
    /// Send a data-only message to a topic (for silent updates)
    /// </summary>
    Task<string> SendDataToTopicAsync(string topic, Dictionary<string, string> data);
    
    /// <summary>
    /// Subscribe a device token to a topic
    /// </summary>
    Task<bool> SubscribeToTopicAsync(string deviceToken, string topic);
    
    /// <summary>
    /// Unsubscribe a device token from a topic
    /// </summary>
    Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic);
    
    /// <summary>
    /// Send a message to multiple device tokens
    /// </summary>
    Task<BatchResponse> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
}

public class BatchResponse
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedTokens { get; set; } = new();
}
```

#### 1.3 Implement FCM Service

**File**: `AppInfra/Services/FirebaseMessagingService.cs`

```csharp
using AppCore.Interfaces;
using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Logging;

namespace AppInfra.Services;

public class FirebaseMessagingService : IFirebaseMessagingService
{
    private readonly ILogger<FirebaseMessagingService> _logger;
    
    public FirebaseMessagingService(ILogger<FirebaseMessagingService> logger)
    {
        _logger = logger;
    }
    
    public async Task<string> SendToDeviceAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                // Web push options
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        RequireInteraction = false
                    }
                }
            };
            
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent FCM message: {MessageId}", response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM message to device: {Token}", deviceToken);
            throw;
        }
    }
    
    public async Task<string> SendToTopicAsync(string topic, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new Message()
            {
                Topic = topic,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data,
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        RequireInteraction = false
                    }
                }
            };
            
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent FCM message to topic {Topic}: {MessageId}", topic, response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM message to topic: {Topic}", topic);
            throw;
        }
    }
    
    public async Task<string> SendDataToTopicAsync(string topic, Dictionary<string, string> data)
    {
        try
        {
            var message = new Message()
            {
                Topic = topic,
                Data = data
            };
            
            var response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
            _logger.LogInformation("Successfully sent data message to topic {Topic}: {MessageId}", topic, response);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data message to topic: {Topic}", topic);
            throw;
        }
    }
    
    public async Task<bool> SubscribeToTopicAsync(string deviceToken, string topic)
    {
        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .SubscribeToTopicAsync(new List<string> { deviceToken }, topic);
            
            if (response.SuccessCount > 0)
            {
                _logger.LogInformation("Device subscribed to topic {Topic}", topic);
                return true;
            }
            
            _logger.LogWarning("Failed to subscribe device to topic {Topic}: {Errors}", 
                topic, string.Join(", ", response.Errors.Select(e => e.Reason)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to topic: {Topic}", topic);
            return false;
        }
    }
    
    public async Task<bool> UnsubscribeFromTopicAsync(string deviceToken, string topic)
    {
        try
        {
            var response = await FirebaseMessaging.DefaultInstance
                .UnsubscribeFromTopicAsync(new List<string> { deviceToken }, topic);
            
            if (response.SuccessCount > 0)
            {
                _logger.LogInformation("Device unsubscribed from topic {Topic}", topic);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from topic: {Topic}", topic);
            return false;
        }
    }
    
    public async Task<BatchResponse> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
    {
        try
        {
            var message = new MulticastMessage()
            {
                Tokens = deviceTokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };
            
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
            
            var failedTokens = new List<string>();
            for (int i = 0; i < response.Responses.Count; i++)
            {
                if (!response.Responses[i].IsSuccess)
                {
                    failedTokens.Add(deviceTokens[i]);
                }
            }
            
            return new BatchResponse
            {
                SuccessCount = response.SuccessCount,
                FailureCount = response.FailureCount,
                FailedTokens = failedTokens
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send multicast message");
            throw;
        }
    }
}
```

#### 1.4 Create FCM Device Token Management

**New Entity**: `AppCore/Entities/UserDeviceToken.cs`

```csharp
namespace AppCore.Entities;

/// <summary>
/// Stores FCM device tokens for users to enable web push notifications
/// </summary>
public class UserDeviceToken : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string DeviceToken { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // "web" (browser type: chrome, firefox, safari, edge)
    public string? DeviceName { get; set; }
    public DateTime LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
}
```

**Repository Interface**: `AppCore/Interfaces/IUserDeviceTokenRepository.cs`

```csharp
namespace AppCore.Interfaces;

public interface IUserDeviceTokenRepository : IRepository<UserDeviceToken>
{
    Task<List<UserDeviceToken>> GetActiveTokensByUserIdAsync(string userId);
    Task<UserDeviceToken?> GetByTokenAsync(string deviceToken);
    Task DeactivateTokenAsync(string deviceToken);
    Task DeactivateUserTokensAsync(string userId);
}
```

**Repository Implementation**: `AppInfra/Repositories/UserDeviceTokenRepository.cs`

#### 1.5 Update Session Participant Tracking

**Add to LeanParticipant Entity**: Track FCM subscription status

```csharp
public class LeanParticipant : BaseEntity
{
    // ... existing properties ...
    
    // FCM-related fields
    public bool IsSubscribedToFCM { get; set; }
    public DateTime? FCMSubscribedAt { get; set; }
}
```

---

### Phase 2: Create Notification Abstraction Layer

#### 2.1 Create Lean Coffee Notification Service

**Interface**: `AppCore/Interfaces/ILeanCoffeeNotificationService.cs`

```csharp
namespace AppCore.Interfaces;

public interface ILeanCoffeeNotificationService
{
    // Session Events
    Task NotifySessionCreatedAsync(string sessionId, string facilitatorId);
    Task NotifySessionUpdatedAsync(string sessionId);
    Task NotifySessionClosedAsync(string sessionId);
    Task NotifySessionStateChangedAsync(string sessionId, SessionStatus newStatus);
    
    // Participant Events
    Task NotifyParticipantJoinedAsync(string sessionId, string userId);
    Task NotifyParticipantLeftAsync(string sessionId, string userId);
    
    // Topic Events
    Task NotifyTopicAddedAsync(string sessionId, string topicId);
    Task NotifyTopicUpdatedAsync(string sessionId, string topicId);
    Task NotifyTopicStatusChangedAsync(string sessionId, string topicId, TopicStatus newStatus);
    Task NotifyCurrentTopicChangedAsync(string sessionId, string? topicId);
    
    // Vote Events
    Task NotifyVoteCastAsync(string sessionId, string topicId, string userId);
    Task NotifyVoteRemovedAsync(string sessionId, string topicId, string userId);
    
    // Note Events
    Task NotifyNoteAddedAsync(string sessionId, string noteId);
}
```

**Implementation**: `AppCore/Services/LeanCoffeeNotificationService.cs`

```csharp
using AppCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppCore.Services;

public class LeanCoffeeNotificationService : ILeanCoffeeNotificationService
{
    private readonly IFirebaseMessagingService _fcmService;
    private readonly ILeanSessionRepository _sessionRepository;
    private readonly ILeanTopicRepository _topicRepository;
    private readonly ILeanParticipantRepository _participantRepository;
    private readonly IUserDeviceTokenRepository _deviceTokenRepository;
    private readonly ILogger<LeanCoffeeNotificationService> _logger;
    
    public LeanCoffeeNotificationService(
        IFirebaseMessagingService fcmService,
        ILeanSessionRepository sessionRepository,
        ILeanTopicRepository topicRepository,
        ILeanParticipantRepository participantRepository,
        IUserDeviceTokenRepository deviceTokenRepository,
        ILogger<LeanCoffeeNotificationService> logger)
    {
        _fcmService = fcmService;
        _sessionRepository = sessionRepository;
        _topicRepository = topicRepository;
        _participantRepository = participantRepository;
        _deviceTokenRepository = deviceTokenRepository;
        _logger = logger;
    }
    
    public async Task NotifySessionCreatedAsync(string sessionId, string facilitatorId)
    {
        var session = await _sessionRepository.GetById(sessionId);
        if (session == null) return;
        
        var data = new Dictionary<string, string>
        {
            { "eventType", "session_created" },
            { "sessionId", sessionId },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        // Send to session topic (for anyone subscribed)
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
        
        _logger.LogInformation("Sent FCM notification for session created: {SessionId}", sessionId);
    }
    
    public async Task NotifyTopicAddedAsync(string sessionId, string topicId)
    {
        var topic = await _topicRepository.GetById(topicId);
        if (topic == null) return;
        
        var data = new Dictionary<string, string>
        {
            { "eventType", "topic_added" },
            { "sessionId", sessionId },
            { "topicId", topicId },
            { "topicTitle", topic.Title },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
        
        _logger.LogInformation("Sent FCM notification for topic added: {TopicId} in session {SessionId}", 
            topicId, sessionId);
    }
    
    public async Task NotifyVoteCastAsync(string sessionId, string topicId, string userId)
    {
        var topic = await _topicRepository.GetById(topicId);
        if (topic == null) return;
        
        var data = new Dictionary<string, string>
        {
            { "eventType", "vote_cast" },
            { "sessionId", sessionId },
            { "topicId", topicId },
            { "userId", userId },
            { "voteCount", topic.VoteCount.ToString() },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
    }
    
    public async Task NotifyCurrentTopicChangedAsync(string sessionId, string? topicId)
    {
        var data = new Dictionary<string, string>
        {
            { "eventType", "current_topic_changed" },
            { "sessionId", sessionId },
            { "topicId", topicId ?? "" },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
    }
    
    public async Task NotifyTopicStatusChangedAsync(string sessionId, string topicId, TopicStatus newStatus)
    {
        var data = new Dictionary<string, string>
        {
            { "eventType", "topic_status_changed" },
            { "sessionId", sessionId },
            { "topicId", topicId },
            { "newStatus", newStatus.ToString() },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
    }
    
    public async Task NotifySessionStateChangedAsync(string sessionId, SessionStatus newStatus)
    {
        var data = new Dictionary<string, string>
        {
            { "eventType", "session_state_changed" },
            { "sessionId", sessionId },
            { "newStatus", newStatus.ToString() },
            { "timestamp", DateTime.UtcNow.ToString("O") }
        };
        
        await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
    }
    
    // Additional implementation for other methods...
    public async Task NotifySessionUpdatedAsync(string sessionId) { /* Implementation */ }
    public async Task NotifySessionClosedAsync(string sessionId) { /* Implementation */ }
    public async Task NotifyParticipantJoinedAsync(string sessionId, string userId) { /* Implementation */ }
    public async Task NotifyParticipantLeftAsync(string sessionId, string userId) { /* Implementation */ }
    public async Task NotifyTopicUpdatedAsync(string sessionId, string topicId) { /* Implementation */ }
    public async Task NotifyVoteRemovedAsync(string sessionId, string topicId, string userId) { /* Implementation */ }
    public async Task NotifyNoteAddedAsync(string sessionId, string noteId) { /* Implementation */ }
}
```

---

### Phase 3: Update Controllers to Use FCM

#### 3.1 Replace SignalR Dependencies

**Before** (LeanTopicsController.cs):
```csharp
private readonly IHubContext<LeanSessionHub> _hubContext;

public LeanTopicsController(IHubContext<LeanSessionHub> hubContext) { ... }

// In methods:
await _hubContext.Clients.Group($"session_{sessionId}")
    .SendAsync("TopicAdded", new { ... });
```

**After**:
```csharp
private readonly ILeanCoffeeNotificationService _notificationService;

public LeanTopicsController(ILeanCoffeeNotificationService notificationService) { ... }

// In methods:
await _notificationService.NotifyTopicAddedAsync(sessionId, topicId);
```

#### 3.2 Create Device Token Management Controller

**New Controller**: `IteraWebApi/Controllers/DeviceTokensController.cs`

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly IFirebaseMessagingService _fcmService;
    
    [HttpPost("RegisterToken")]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var userId = User.FindFirst("user_id")?.Value;
        
        var token = new UserDeviceToken
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            DeviceToken = request.Token,
            DeviceType = request.DeviceType,
            DeviceName = request.DeviceName,
            LastUsedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };
        
        await _tokenRepository.Add(token);
        return Ok(new { success = true });
    }
    
    [HttpPost("SubscribeToSession")]
    public async Task<IActionResult> SubscribeToSession([FromBody] SubscribeToSessionRequest request)
    {
        var userId = User.FindFirst("user_id")?.Value;
        var tokens = await _tokenRepository.GetActiveTokensByUserIdAsync(userId);
        
        foreach (var token in tokens)
        {
            await _fcmService.SubscribeToTopicAsync(token.DeviceToken, $"session_{request.SessionId}");
        }
        
        return Ok(new { success = true, subscribedDevices = tokens.Count });
    }
    
    [HttpPost("UnsubscribeFromSession")]
    public async Task<IActionResult> UnsubscribeFromSession([FromBody] UnsubscribeFromSessionRequest request)
    {
        var userId = User.FindFirst("user_id")?.Value;
        var tokens = await _tokenRepository.GetActiveTokensByUserIdAsync(userId);
        
        foreach (var token in tokens)
        {
            await _fcmService.UnsubscribeFromTopicAsync(token.DeviceToken, $"session_{request.SessionId}");
        }
        
        return Ok(new { success = true });
    }
}
```

---

### Phase 4: Update Program.cs and Remove SignalR

#### 4.1 Register New Services

**In Program.cs**, add before `var app = builder.Build();`:

```csharp
// Register FCM Services
builder.Services.AddScoped<IFirebaseMessagingService, FirebaseMessagingService>();
builder.Services.AddScoped<ILeanCoffeeNotificationService, LeanCoffeeNotificationService>();
builder.Services.AddScoped<IUserDeviceTokenRepository, UserDeviceTokenRepository>();

// Register UserDeviceToken with Marten
options.Schema.For<AppCore.Entities.UserDeviceToken>().Identity(x => x.Id);
options.Schema.For<AppCore.Entities.UserDeviceToken>()
    .Index(x => x.UserId)
    .Index(x => x.DeviceToken)
    .Index(x => x.IsActive);
```

#### 4.2 Remove SignalR Configuration

**Remove these lines from Program.cs**:

```csharp
// DELETE:
// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// DELETE:
// Map SignalR Hub
app.MapHub<IteraWebApi.Hubs.LeanSessionHub>("/hubs/lean-session");
```

**Update CORS** (remove SignalR comment):
```csharp
policy.WithOrigins(...)
      .AllowAnyHeader()
      .AllowAnyMethod()
      .AllowCredentials() // Keep this for cookies/auth
      .SetIsOriginAllowedToAllowWildcardSubdomains();
```

#### 4.3 Delete SignalR Hub

**Delete file**: `IteraWebApi/Hubs/LeanSessionHub.cs`

---

### Phase 5: Update Domain Services

Update these service classes to inject and use `ILeanCoffeeNotificationService`:

1. **LeanSessionService** - Add notifications for:
   - Session creation
   - Session updates
   - Session state changes

2. **LeanTopicService** - Add notifications for:
   - Topic creation
   - Topic updates
   - Topic status changes
   - Current topic changes

3. **LeanParticipantService** - Add notifications for:
   - Participant joined
   - Participant left

**Example Pattern**:
```csharp
public class LeanTopicService
{
    private readonly ILeanCoffeeNotificationService _notificationService;
    
    public async Task<AppResult<LeanTopic>> AddTopicAsync(AddTopicCommand command)
    {
        // ... existing logic ...
        
        // Send FCM notification
        await _notificationService.NotifyTopicAddedAsync(command.SessionId, topic.Id);
        
        return AppResult<LeanTopic>.SuccessResult(topic);
    }
}
```

---

## FCM Topic Naming Convention

Use consistent topic naming for Firebase Cloud Messaging:

- **Session-wide updates**: `session_{sessionId}`
  - Example: `session_abc123xyz`
  - All participants subscribe to this topic

- **User-specific**: Use device tokens directly
  - Don't create per-user topics
  - Query user's active device tokens and send multicast

---

## Data Message Structure

All FCM data messages should follow this schema:

```json
{
  "eventType": "topic_added | vote_cast | session_state_changed | ...",
  "sessionId": "string",
  "topicId": "string (optional)",
  "userId": "string (optional)",
  "timestamp": "ISO 8601 timestamp",
  "additionalData": "varies by event type"
}
```

Frontend clients will parse the `eventType` and handle accordingly.

---

## Migration Checklist

### Backend Tasks

- [ ] Create `IFirebaseMessagingService` interface
- [ ] Implement `FirebaseMessagingService`
- [ ] Create `UserDeviceToken` entity
- [ ] Create `IUserDeviceTokenRepository` interface
- [ ] Implement `UserDeviceTokenRepository`
- [ ] Update `LeanParticipant` entity with FCM fields
- [ ] Create `ILeanCoffeeNotificationService` interface
- [ ] Implement `LeanCoffeeNotificationService`
- [ ] Create `DeviceTokensController`
- [ ] Update `LeanSessionsController` to use notification service
- [ ] Update `LeanTopicsController` to use notification service
- [ ] Update `LeanParticipantsController` to use notification service
- [ ] Update `LeanSessionService` to send notifications
- [ ] Update `LeanTopicService` to send notifications
- [ ] Update `LeanParticipantService` to send notifications
- [ ] Register all new services in `Program.cs`
- [ ] Add Marten configuration for `UserDeviceToken`
- [ ] Remove SignalR configuration from `Program.cs`
- [ ] Remove SignalR hub endpoint mapping
- [ ] Delete `LeanSessionHub.cs`
- [ ] Remove SignalR package references if no longer needed
- [ ] Test all notification scenarios

---

## Testing Strategy

### Unit Tests

Create tests for:
1. `FirebaseMessagingService` - Mock FCM SDK
2. `LeanCoffeeNotificationService` - Mock dependencies
3. Device token repository operations

### Integration Tests

1. **FCM Message Delivery**:
   - Subscribe device to topic
   - Send message to topic
   - Verify message received

2. **Topic Management**:
   - Subscribe multiple devices to session
   - Send notification
   - Verify all devices receive message

3. **Device Token Lifecycle**:
   - Register token
   - Deactivate token
   - Verify no messages sent to inactive tokens

### Manual Testing

1. Register device token from Angular app
2. Join a lean coffee session
3. Verify real-time updates for:
   - New topics added
   - Votes cast
   - Topic status changes
   - Session state changes

---

## Rollback Plan

If issues arise during migration:

1. **Keep SignalR hub temporarily** - Run both systems in parallel during testing
2. **Feature flag** - Add configuration to toggle between SignalR and FCM
3. **Gradual migration** - Migrate one event type at a time
4. **Monitor errors** - Log all FCM failures for investigation

**Feature Flag Example**:
```csharp
// In appsettings.json
{
  "FeatureFlags": {
    "UseFCM": false  // Set to true when ready to switch
  }
}

// In services
if (_configuration.GetValue<bool>("FeatureFlags:UseFCM"))
{
    await _notificationService.NotifyTopicAddedAsync(...);
}
else
{
    await _hubContext.Clients.Group(...).SendAsync(...);
}
```

---

## Security Considerations

1. **Device Token Storage**:
   - Store device tokens securely
   - Associate with authenticated users only
   - Implement token expiration/refresh logic

2. **Topic Access Control**:
   - Only subscribe users to sessions they have access to
   - Verify session participation before allowing subscription
   - Implement participant validation in subscription endpoints

3. **Message Content**:
   - Use data-only messages for sensitive information
   - Don't include sensitive data in notification titles/bodies
   - Client should fetch full data via API calls

4. **Rate Limiting**:
   - Implement rate limiting on notification sends
   - Prevent notification spam
   - Monitor FCM quotas

---

## Performance Considerations

1. **Batch Operations**:
   - Use multicast for sending to multiple devices
   - Batch subscription operations when possible

2. **Caching**:
   - Cache active device tokens per user
   - Invalidate cache on token updates

3. **Async Processing**:
   - Send FCM messages asynchronously (fire-and-forget)
   - Don't block controller responses waiting for FCM
   - Consider using background jobs for large batches

4. **Error Handling**:
   - Implement retry logic with exponential backoff
   - Handle invalid token errors (mark as inactive)
   - Log failures for monitoring

---

## Future Enhancements

1. **Analytics**:
   - Track notification delivery rates
   - Monitor user engagement with notifications
   - A/B test notification content

2. **Advanced Features**:
   - User notification preferences
   - Notification history/inbox
   - Rich media notifications
   - Action buttons in notifications

3. **Enhanced Web Support**:
   - Progressive Web App (PWA) notifications
   - Desktop web app notifications
   - Multiple browser support optimization
   - Notification permission management UI

---

## Documentation Updates Required

After migration, update:

1. **API Documentation** (`Designs/openapi.json`):
   - Add device token endpoints
   - Document FCM subscription flow
   - Remove SignalR references

2. **Architecture Diagrams**:
   - Update to show FCM integration
   - Document message flow

3. **Developer Setup Guide**:
   - Firebase project setup
   - FCM configuration steps
   - Testing with Firebase console

---

## Questions & Decisions

### Q: Should we support both notification and data-only messages?
**A**: Use data-only messages for real-time updates (silent). Use notification messages only for important events when user is not in the app.

### Q: How do we handle offline users?
**A**: FCM stores messages for up to 4 weeks. When user comes online, they'll receive pending messages. Consider implementing a "sync" endpoint to fetch missed updates.

### Q: What about web browser support?
**A**: Firebase Cloud Messaging supports web browsers via Service Workers. Frontend implementation will handle this.

### Q: Should we keep message history?
**A**: No, FCM is for real-time delivery only. If users need history, they should fetch via REST API.

---

## Related Documents

- [Lean Coffee Domain Spec](../Designs/lean_coffee.md)
- [Firebase Authentication Setup](../AUTHENTICATION_SETUP.md)
- Frontend implementation plan (to be created)

---

## Approval & Timeline

**Estimated Effort**: 3-4 days
- Day 1: Infrastructure setup (FCM service, repositories)
- Day 2: Notification service implementation
- Day 3: Controller updates, remove SignalR
- Day 4: Testing and documentation

**Dependencies**:
- Firebase project already configured ✅
- Firebase Admin SDK already installed ✅
- Authentication working ✅

**Sign-off Required**: Tech Lead, Product Owner

---

*End of Backend Migration Plan*
