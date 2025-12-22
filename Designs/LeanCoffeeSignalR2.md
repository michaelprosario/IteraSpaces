# Lean Coffee Backend Implementation Plan with SignalR

## Overview
This document outlines the backend implementation for a real-time Lean Coffee session view using SignalR for bidirectional communication between the ASP.NET Core WebAPI and Angular frontend.

## Architecture Goals
- **Real-time Updates**: All participants see changes immediately
- **Vote Integrity**: Users can only vote once per topic
- **Participant Tracking**: Track active participants in each session
- **Scalable Design**: Support multiple concurrent sessions
- **Clean Architecture**: Maintain separation of concerns (AppCore, AppInfra, IteraWebApi)
- **Consistent API**: All controllers use HTTP POST with descriptive action names

---

## 1. SignalR Hub Implementation

### 1.1 LeanSessionHub (IteraWebApi/Hubs/LeanSessionHub.cs)

```csharp
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IteraWebApi.Hubs;

[Authorize]
public class LeanSessionHub : Hub
{
    private readonly ILeanSessionRepository _sessionRepository;
    private readonly ILeanParticipantRepository _participantRepository;
    private readonly ILeanTopicRepository _topicRepository;
    private readonly ILeanTopicVoteRepository _voteRepository;
    private readonly ILogger<LeanSessionHub> _logger;

    public LeanSessionHub(
        ILeanSessionRepository sessionRepository,
        ILeanParticipantRepository participantRepository,
        ILeanTopicRepository topicRepository,
        ILeanTopicVoteRepository voteRepository,
        ILogger<LeanSessionHub> logger)
    {
        _sessionRepository = sessionRepository;
        _participantRepository = participantRepository;
        _topicRepository = topicRepository;
        _voteRepository = voteRepository;
        _logger = logger;
    }

    // ===== Connection Management =====
    
    /// <summary>
    /// Join a lean coffee session and receive real-time updates
    /// </summary>
    public async Task<AppResult> JoinSession(string sessionId, string userId)
    {
        try
        {
            // Validate session exists
            var session = await _sessionRepository.GetById(sessionId);
            if (session == null)
                return AppResult.Failure("Session not found");

            // Add user to SignalR group for this session
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Track participant (create or update)
            var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
            if (participant == null)
            {
                var newParticipant = new LeanParticipant
                {
                    Id = Guid.NewGuid().ToString(),
                    LeanSessionId = sessionId,
                    UserId = userId,
                    Role = ParticipantRole.Participant,
                    JoinedAt = DateTime.UtcNow,
                    IsActive = true
                };
                await _participantRepository.Add(newParticipant);
            }
            else
            {
                // Reactivate if they rejoined
                participant.IsActive = true;
                participant.LeftAt = null;
                await _participantRepository.Update(participant);
            }

            // Notify others in the session
            await Clients.OthersInGroup($"session_{sessionId}")
                .SendAsync("ParticipantJoined", new { sessionId, userId, timestamp = DateTime.UtcNow });

            _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining session {SessionId}", sessionId);
            return AppResult.Failure($"Failed to join session: {ex.Message}");
        }
    }

    /// <summary>
    /// Leave a lean coffee session
    /// </summary>
    public async Task<AppResult> LeaveSession(string sessionId, string userId)
    {
        try
        {
            // Update participant status
            var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
            if (participant != null)
            {
                participant.IsActive = false;
                participant.LeftAt = DateTime.UtcNow;
                await _participantRepository.Update(participant);
            }

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Notify others
            await Clients.OthersInGroup($"session_{sessionId}")
                .SendAsync("ParticipantLeft", new { sessionId, userId, timestamp = DateTime.UtcNow });

            _logger.LogInformation("User {UserId} left session {SessionId}", userId, sessionId);
            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving session {SessionId}", sessionId);
            return AppResult.Failure($"Failed to leave session: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle disconnect (user closes browser, loses connection, etc.)
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up participant status
        // Note: We'd need to track ConnectionId -> (SessionId, UserId) mapping
        // Consider using a distributed cache like Redis for connection tracking
        
        _logger.LogInformation("Connection {ConnectionId} disconnected", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // ===== Real-time Event Notifications =====
    
    /// <summary>
    /// Notify all participants that a topic was added
    /// </summary>
    public async Task<AppResult> NotifyTopicAdded(string sessionId, string topicId)
    {
        try
        {
            var topic = await _topicRepository.GetById(topicId);
            if (topic == null)
                return AppResult.Failure("Topic not found");

            await Clients.Group($"session_{sessionId}")
                .SendAsync("TopicAdded", new 
                { 
                    sessionId, 
                    topicId, 
                    topic,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic added");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a topic was edited
    /// </summary>
    public async Task<AppResult> NotifyTopicEdited(string topicId)
    {
        try
        {
            var topic = await _topicRepository.GetById(topicId);
            if (topic == null)
                return AppResult.Failure("Topic not found");

            await Clients.Group($"session_{topic.LeanSessionId}")
                .SendAsync("TopicEdited", new 
                { 
                    topicId, 
                    sessionId = topic.LeanSessionId,
                    topic,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic edited");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a topic was deleted
    /// </summary>
    public async Task<AppResult> NotifyTopicDeleted(string topicId, string sessionId)
    {
        try
        {
            await Clients.Group($"session_{sessionId}")
                .SendAsync("TopicDeleted", new 
                { 
                    topicId, 
                    sessionId,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic deleted");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a vote was cast
    /// Includes updated vote count
    /// </summary>
    public async Task<AppResult> NotifyVoteCast(string topicId, string sessionId, int newVoteCount, string userId)
    {
        try
        {
            await Clients.Group($"session_{sessionId}")
                .SendAsync("VoteCast", new 
                { 
                    topicId, 
                    sessionId,
                    voteCount = newVoteCount,
                    userId,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying vote cast");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a vote was removed
    /// </summary>
    public async Task<AppResult> NotifyVoteRemoved(string topicId, string sessionId, int newVoteCount, string userId)
    {
        try
        {
            await Clients.Group($"session_{sessionId}")
                .SendAsync("VoteRemoved", new 
                { 
                    topicId, 
                    sessionId,
                    voteCount = newVoteCount,
                    userId,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying vote removed");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that session status changed
    /// </summary>
    public async Task<AppResult> NotifySessionStatusChanged(string sessionId, SessionStatus newStatus)
    {
        try
        {
            await Clients.Group($"session_{sessionId}")
                .SendAsync("SessionStatusChanged", new 
                { 
                    sessionId,
                    status = newStatus,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying session status changed");
            return AppResult.Failure(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that topic status changed
    /// </summary>
    public async Task<AppResult> NotifyTopicStatusChanged(string topicId, string sessionId, TopicStatus newStatus)
    {
        try
        {
            await Clients.Group($"session_{sessionId}")
                .SendAsync("TopicStatusChanged", new 
                { 
                    topicId,
                    sessionId,
                    status = newStatus,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic status changed");
            return AppResult.Failure(ex.Message);
        }
    }
}
```

---

## 2. Service Layer Enhancements

### 2.1 Enhanced LeanParticipantService

**Update existing LeanParticipantService** (AppCore/Services/LeanParticipantService.cs) to add additional methods:

```csharp
// Add these methods to existing LeanParticipantService class

public async Task<AppResult<LeanParticipant>> JoinSessionAsync(string sessionId, string userId, ParticipantRole role)
{
    // Check if session exists
    var session = await _sessionRepository.GetById(sessionId);
    if (session == null)
    {
        return AppResult<LeanParticipant>.FailureResult(
            "Session not found",
            "SESSION_NOT_FOUND");
    }

    // Check if already an active participant
    var existingParticipant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
    if (existingParticipant != null)
    {
        // Reactivate if previously left
        if (!existingParticipant.IsActive)
        {
            existingParticipant.IsActive = true;
            existingParticipant.LeftAt = null;
            existingParticipant.UpdatedAt = DateTime.UtcNow;
            await _participantRepository.Update(existingParticipant);
        }
        return AppResult<LeanParticipant>.SuccessResult(existingParticipant);
    }

    // Create new participant
    var participant = new LeanParticipant
    {
        Id = Guid.NewGuid().ToString(),
        LeanSessionId = sessionId,
        UserId = userId,
        Role = role,
        JoinedAt = DateTime.UtcNow,
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = userId
    };

    await _participantRepository.Add(participant);
    
    return AppResult<LeanParticipant>.SuccessResult(
        participant,
        "Participant joined successfully");
}

public async Task<AppResult<LeanParticipant>> LeaveSessionAsync(string sessionId, string userId)
{
    var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
    if (participant == null || !participant.IsActive)
    {
        return AppResult<LeanParticipant>.FailureResult(
            "Active participant not found",
            "PARTICIPANT_NOT_FOUND");
    }

    participant.IsActive = false;
    participant.LeftAt = DateTime.UtcNow;
    participant.UpdatedAt = DateTime.UtcNow;
    await _participantRepository.Update(participant);

    return AppResult<LeanParticipant>.SuccessResult(
        participant,
        "Participant left session");
}

public async Task<AppResult<IEnumerable<LeanParticipant>>> GetActiveParticipantsAsync(string sessionId)
{
    var participants = await _participantRepository.GetActiveParticipantsBySessionAsync(sessionId);
    return AppResult<IEnumerable<LeanParticipant>>.SuccessResult(participants);
}

public async Task<AppResult<bool>> IsUserInSessionAsync(string sessionId, string userId)
{
    var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
    return AppResult<bool>.SuccessResult(participant != null && participant.IsActive);
}
```

### 2.2 Enhanced LeanSessionService

**Update existing LeanSessionService** (AppCore/Services/LeanSessionService.cs) to add session status change:

```csharp
// Add this method to existing LeanSessionService class

public async Task<AppResult<LeanSession>> ChangeSessionStatusAsync(string sessionId, SessionStatus newStatus, string userId)
{
    var session = await _sessionRepository.GetById(sessionId);
    if (session == null)
    {
        return AppResult<LeanSession>.FailureResult(
            "Session not found",
            "SESSION_NOT_FOUND");
    }

    session.Status = newStatus;
    
    // Set timestamps based on status
    if (newStatus == SessionStatus.InProgress && !session.ActualStartTime.HasValue)
    {
        session.ActualStartTime = DateTime.UtcNow;
    }
    else if (newStatus == SessionStatus.Completed && !session.ActualEndTime.HasValue)
    {
        session.ActualEndTime = DateTime.UtcNow;
    }

    session.UpdatedAt = DateTime.UtcNow;
    session.UpdatedBy = userId;
    
    await _sessionRepository.Update(session);

    return AppResult<LeanSession>.SuccessResult(
        session,
        $"Session status changed to {newStatus}");
}
```

### 2.3 Enhanced LeanTopicService for Voting

**Update existing LeanTopicService** (AppCore/Services/LeanTopicService.cs) to add vote removal:

```csharp
// Add this method to existing LeanTopicService class

public async Task<AppResult<LeanTopic>> RemoveVoteAsync(string topicId, string userId)
{
    // Check if vote exists
    var existingVote = await _voteRepository.GetByTopicAndUserIdAsync(topicId, userId);
    if (existingVote == null)
    {
        return AppResult<LeanTopic>.FailureResult(
            "Vote not found",
            "VOTE_NOT_FOUND");
    }

    // Remove vote
    await _voteRepository.Delete(existingVote.Id);

    // Update topic vote count
    var topic = await _topicRepository.GetById(topicId);
    if (topic != null)
    {
        topic.VoteCount = await _voteRepository.GetVoteCountForTopicAsync(topicId);
        topic.UpdatedAt = DateTime.UtcNow;
        await _topicRepository.Update(topic);
        
        return AppResult<LeanTopic>.SuccessResult(
            topic,
            "Vote removed successfully");
    }

    return AppResult<LeanTopic>.FailureResult(
        "Topic not found after vote removal",
        "TOPIC_NOT_FOUND");
}

public async Task<AppResult<bool>> HasUserVotedAsync(string topicId, string userId)
{
    var vote = await _voteRepository.GetByTopicAndUserIdAsync(topicId, userId);
    return AppResult<bool>.SuccessResult(vote != null);
}

public async Task<AppResult<IEnumerable<LeanTopicVote>>> GetUserVotesForSessionAsync(string sessionId, string userId)
{
    var votes = await _voteRepository.GetBySessionAndUserIdAsync(sessionId, userId);
    return AppResult<IEnumerable<LeanTopicVote>>.SuccessResult(votes);
}
```

**Note**: The existing `VoteForLeanTopicAsync` method already handles vote integrity by checking for existing votes.

---

## 3. Repository Layer Updates

### 3.1 ILeanParticipantRepository Extensions (AppCore/Interfaces/ILeanParticipantRepository.cs)

Add these methods to the existing repository interface:

```csharp
Task<LeanParticipant?> GetBySessionAndUserIdAsync(string sessionId, string userId);
Task<IEnumerable<LeanParticipant>> GetActiveParticipantsBySessionAsync(string sessionId);
```

### 3.2 LeanParticipantRepository Implementation (AppInfra/Repositories/LeanParticipantRepository.cs)

Add these methods to the existing repository implementation:

```csharp
public async Task<LeanParticipant?> GetBySessionAndUserIdAsync(string sessionId, string userId)
{
    using var session = _store.QuerySession();
    return await session.Query<LeanParticipant>()
        .Where(p => p.LeanSessionId == sessionId && p.UserId == userId)
        .FirstOrDefaultAsync();
}

public async Task<IEnumerable<LeanParticipant>> GetActiveParticipantsBySessionAsync(string sessionId)
{
    using var session = _store.QuerySession();
    return await session.Query<LeanParticipant>()
        .Where(p => p.LeanSessionId == sessionId && p.IsActive)
        .OrderBy(p => p.JoinedAt)
        .ToListAsync();
}
```

### 3.3 ILeanTopicVoteRepository Extensions (AppCore/Interfaces/ILeanTopicVoteRepository.cs)

Add this method to the existing repository interface:

```csharp
Task<IEnumerable<LeanTopicVote>> GetBySessionAndUserIdAsync(string sessionId, string userId);
```

### 3.4 LeanTopicVoteRepository Implementation (AppInfra/Repositories/LeanTopicVoteRepository.cs)

Add this method to the existing repository implementation:

```csharp
public async Task<IEnumerable<LeanTopicVote>> GetBySessionAndUserIdAsync(string sessionId, string userId)
{
    using var session = _store.QuerySession();
    return await session.Query<LeanTopicVote>()
        .Where(v => v.LeanSessionId == sessionId && v.UserId == userId)
        .ToListAsync();
}
```

**Note**: The repository methods should follow the existing pattern in the codebase, returning entities directly or null, not wrapped in AppResult. The service layer handles AppResult wrapping.

---

## 4. Controller Integration

All controllers use **HTTP POST** with descriptive action names for consistency with the existing codebase pattern.

### 4.1 Enhanced LeanTopicsController (IteraWebApi/Controllers/LeanTopicsController.cs)

Update to trigger SignalR notifications using **services only**:

```csharp
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using IteraWebApi.Hubs;

namespace IteraWebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeanTopicsController : ControllerBase
{
    private readonly LeanTopicService _topicService;
    private readonly IHubContext<LeanSessionHub> _hubContext;
    private readonly ILogger<LeanTopicsController> _logger;

    public LeanTopicsController(
        LeanTopicService topicService,
        IHubContext<LeanSessionHub> hubContext,
        ILogger<LeanTopicsController> logger)
    {
        _topicService = topicService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("CreateTopic")]
    public async Task<IActionResult> CreateTopic([FromBody] LeanTopic topic)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        
        var command = new AddEntityCommand<LeanTopic>(topic)
        {
            UserId = userId
        };

        var result = await _topicService.AddTopicAsync(command);
        
        if (result.Success && result.Data != null)
        {
            // Notify all session participants via SignalR
            await _hubContext.Clients.Group($"session_{result.Data.LeanSessionId}")
                .SendAsync("TopicAdded", new 
                { 
                    sessionId = result.Data.LeanSessionId,
                    topicId = result.Data.Id,
                    topic = result.Data,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("VoteForTopic")]
    public async Task<IActionResult> VoteForTopic([FromBody] VoteForLeanTopicCommand command)
    {
        var result = await _topicService.VoteForLeanTopicAsync(command);
        
        if (result.Success && result.Data != null)
        {
            // Get updated topic from vote result to send current vote count
            var topicQuery = new GetEntityByIdQuery(command.LeanTopicId);
            var topicResult = await _topicService.GetEntityByIdAsync(topicQuery);
            
            if (topicResult.Success && topicResult.Data != null)
            {
                await _hubContext.Clients.Group($"session_{command.LeanSessionId}")
                    .SendAsync("VoteCast", new 
                    { 
                        topicId = command.LeanTopicId,
                        sessionId = command.LeanSessionId,
                        voteCount = topicResult.Data.VoteCount,
                        userId = command.UserId,
                        timestamp = DateTime.UtcNow 
                    });
            }
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("RemoveVote")]
    public async Task<IActionResult> RemoveVote([FromBody] RemoveVoteCommand command)
    {
        var result = await _topicService.RemoveVoteAsync(command.TopicId, command.UserId);
        
        if (result.Success && result.Data != null)
        {
            await _hubContext.Clients.Group($"session_{command.SessionId}")
                .SendAsync("VoteRemoved", new 
                { 
                    topicId = command.TopicId,
                    sessionId = command.SessionId,
                    voteCount = result.Data.VoteCount,
                    userId = command.UserId,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("UpdateTopicStatus")]
    public async Task<IActionResult> UpdateTopicStatus([FromBody] SetTopicStatusCommand command)
    {
        var result = await _topicService.SetTopicStatusAsync(command);
        
        if (result.Success && result.Data != null)
        {
            await _hubContext.Clients.Group($"session_{result.Data.LeanSessionId}")
                .SendAsync("TopicStatusChanged", new 
                { 
                    topicId = command.TopicId,
                    sessionId = result.Data.LeanSessionId,
                    status = result.Data.Status,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ... other endpoints like Update, Delete, etc.
}
```

### 4.2 Enhanced LeanSessionsController (IteraWebApi/Controllers/LeanSessionsController.cs)

Add SignalR notification for session status changes:

```csharp
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using IteraWebApi.Hubs;

namespace IteraWebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeanSessionsController : ControllerBase
{
    private readonly LeanSessionService _sessionService;
    private readonly LeanSessionQueryService _queryService;
    private readonly IHubContext<LeanSessionHub> _hubContext;

    public LeanSessionsController(
        LeanSessionService sessionService,
        LeanSessionQueryService queryService,
        IHubContext<LeanSessionHub> hubContext)
    {
        _sessionService = sessionService;
        _queryService = queryService;
        _hubContext = hubContext;
    }

    // ... existing methods ...

    [HttpPost("ChangeSessionStatus")]
    public async Task<IActionResult> ChangeSessionStatus([FromBody] ChangeSessionStatusCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        var result = await _sessionService.ChangeSessionStatusAsync(
            command.SessionId, 
            command.NewStatus, 
            userId);
        
        if (result.Success && result.Data != null)
        {
            await _hubContext.Clients.Group($"session_{command.SessionId}")
                .SendAsync("SessionStatusChanged", new 
                { 
                    sessionId = command.SessionId,
                    status = command.NewStatus,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ... other endpoints
}
```

### 4.3 Enhanced LeanParticipantsController (IteraWebApi/Controllers/LeanParticipantsController.cs)

Add SignalR notifications for participant join/leave:

```csharp
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using IteraWebApi.Hubs;

namespace IteraWebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class LeanParticipantsController : ControllerBase
{
    private readonly LeanParticipantService _participantService;
    private readonly IHubContext<LeanSessionHub> _hubContext;

    public LeanParticipantsController(
        LeanParticipantService participantService,
        IHubContext<LeanSessionHub> hubContext)
    {
        _participantService = participantService;
        _hubContext = hubContext;
    }

    [HttpPost("JoinSession")]
    public async Task<IActionResult> JoinSession([FromBody] JoinSessionCommand command)
    {
        var result = await _participantService.JoinSessionAsync(
            command.SessionId, 
            command.UserId, 
            command.Role);
        
        if (result.Success && result.Data != null)
        {
            await _hubContext.Clients.Group($"session_{command.SessionId}")
                .SendAsync("ParticipantJoined", new 
                { 
                    sessionId = command.SessionId,
                    userId = command.UserId,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("LeaveSession")]
    public async Task<IActionResult> LeaveSession([FromBody] LeaveSessionCommand command)
    {
        var result = await _participantService.LeaveSessionAsync(
            command.SessionId, 
            command.UserId);
        
        if (result.Success && result.Data != null)
        {
            await _hubContext.Clients.Group($"session_{command.SessionId}")
                .SendAsync("ParticipantLeft", new 
                { 
                    sessionId = command.SessionId,
                    userId = command.UserId,
                    timestamp = DateTime.UtcNow 
                });
        }

        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("GetActiveParticipants")]
    public async Task<IActionResult> GetActiveParticipants([FromBody] GetActiveParticipantsQuery query)
    {
        var result = await _participantService.GetActiveParticipantsAsync(query.SessionId);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // ... other endpoints
}
```

### 4.4 New DTOs for Controllers (AppCore/DTOs)

Add these command/query classes to **AppCore/DTOs** (suggest creating a new file: LeanParticipantCommands.cs):

```csharp
using AppCore.Entities;

namespace AppCore.DTOs;

public class JoinSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
}

public class LeaveSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class GetActiveParticipantsQuery
{
    public string SessionId { get; set; } = string.Empty;
}

public class RemoveVoteCommand
{
    public string TopicId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class ChangeSessionStatusCommand
{
    public string SessionId { get; set; } = string.Empty;
    public SessionStatus NewStatus { get; set; }
}
```

---

## 5. Program.cs Configuration

### 5.1 Add SignalR and Services to DI Container

```csharp
// Add SignalR
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

// Add Lean Coffee Services (these should already exist)
// LeanSessionService, LeanTopicService, LeanParticipantService are in AppCore/Services
builder.Services.AddScoped<LeanSessionService>();
builder.Services.AddScoped<LeanSessionQueryService>();
builder.Services.AddScoped<LeanTopicService>();
builder.Services.AddScoped<LeanParticipantService>();

// Add Repositories (these should already exist in AppInfra/Repositories)
builder.Services.AddScoped<ILeanSessionRepository, LeanSessionRepository>();
builder.Services.AddScoped<ILeanSessionNoteRepository, LeanSessionNoteRepository>();
builder.Services.AddScoped<ILeanTopicRepository, LeanTopicRepository>();
builder.Services.AddScoped<ILeanTopicVoteRepository, LeanTopicVoteRepository>();
builder.Services.AddScoped<ILeanParticipantRepository, LeanParticipantRepository>();

// Configure CORS to allow SignalR
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // Required for SignalR
    });
});
```

### 5.2 Map SignalR Hub

```csharp
var app = builder.Build();

// ... other middleware

app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<LeanSessionHub>("/hubs/lean-session"); // SignalR endpoint

app.Run();
```

---

## 6. Client Events (For Angular Implementation Reference)

The Angular client should listen for these SignalR events:

### Connection Events
- **ParticipantJoined**: `{ sessionId, userId, timestamp }`
- **ParticipantLeft**: `{ sessionId, userId, timestamp }`

### Topic Events
- **TopicAdded**: `{ sessionId, topicId, topic, timestamp }`
- **TopicEdited**: `{ topicId, sessionId, topic, timestamp }`
- **TopicDeleted**: `{ topicId, sessionId, timestamp }`
- **TopicStatusChanged**: `{ topicId, sessionId, status, timestamp }`

### Voting Events
- **VoteCast**: `{ topicId, sessionId, voteCount, userId, timestamp }`
- **VoteRemoved**: `{ topicId, sessionId, voteCount, userId, timestamp }`

### Session Events
- **SessionStatusChanged**: `{ sessionId, status, timestamp }`

---

## 7. Implementation Checklist

### Phase 1: Core Infrastructure
- [ ] Install SignalR NuGet package (Microsoft.AspNetCore.SignalR)
- [ ] Create LeanSessionHub class with connection management
- [ ] Configure SignalR in Program.cs
- [ ] Update CORS policy for SignalR

### Phase 2: Participant Tracking
- [ ] Extend LeanParticipantService with JoinSession and LeaveSession methods
- [ ] Add repository methods for participant queries (GetBySessionAndUserIdAsync, GetActiveParticipantsBySessionAsync)
- [ ] Create new DTOs (JoinSessionCommand, LeaveSessionCommand, GetActiveParticipantsQuery)
- [ ] Update LeanParticipantsController to use service and trigger SignalR notifications
- [ ] Test join/leave session functionality

### Phase 3: Voting System
- [ ] Extend LeanTopicService with RemoveVoteAsync and helper methods
- [ ] Add repository methods for vote queries (GetBySessionAndUserIdAsync)
- [ ] Create RemoveVoteCommand DTO
- [ ] Update LeanTopicsController to use service for all vote operations
- [ ] Ensure one-vote-per-user constraint (already implemented in VoteForLeanTopicAsync)
- [ ] Test vote integrity under concurrent operations

### Phase 4: Real-time Notifications
- [ ] Integrate SignalR hub context in controllers
- [ ] Add notification calls after topic CRUD operations
- [ ] Add notification calls after vote operations
- [ ] Add notification calls after status changes
- [ ] Test real-time updates across multiple clients

### Phase 5: Connection Management
- [ ] Implement connection tracking (consider Redis)
- [ ] Handle disconnections gracefully
- [ ] Add reconnection logic
- [ ] Implement connection heartbeat monitoring

### Phase 6: Testing
- [ ] Unit tests for services
- [ ] Integration tests for SignalR hub
- [ ] Load testing for multiple concurrent sessions
- [ ] Test vote integrity under concurrent operations

---

## 8. Additional Considerations

### 8.1 Scalability
- **Redis Backplane**: For scaling across multiple servers
  ```csharp
  builder.Services.AddSignalR()
      .AddStackExchangeRedis("connection-string");
  ```

### 8.2 Connection Tracking
- Use Redis or in-memory cache to track ConnectionId → (SessionId, UserId) mapping
- Required for OnDisconnectedAsync to properly clean up participants

### 8.3 Authorization
- Add `[Authorize]` attribute to hub methods
- Validate user has permission to join specific sessions
- Check facilitator role for certain operations

### 8.4 Error Handling
- Implement comprehensive exception handling in hub methods
- Log all errors with correlation IDs
- Return meaningful error messages to clients

### 8.5 Performance
- Use `Groups` efficiently for session-based broadcasting
- Consider pagination for large topic lists
- Implement debouncing for rapid vote changes on client side

### 8.6 Monitoring
- Add Application Insights or similar for SignalR monitoring
- Track connection counts, message rates, errors
- Monitor hub method execution times

---

## 9. Next Steps

1. **Frontend Implementation**: Create Angular service and components (see separate document)
2. **Testing Strategy**: Develop comprehensive test suite
3. **Deployment**: Configure production environment with Redis backplane
4. **Documentation**: API documentation for SignalR events and methods
5. **Security Review**: Ensure proper authentication and authorization

---

## Summary

This implementation provides a complete real-time Lean Coffee backend using SignalR with:
- ✅ Real-time bidirectional communication
- ✅ Vote integrity (one vote per user per topic)
- ✅ Participant tracking and presence
- ✅ Scalable group-based broadcasting
- ✅ Clean architecture adherence (Services → Repositories)
- ✅ Consistent API pattern (all controllers use HTTP POST)
- ✅ Comprehensive error handling and logging

The system is ready for integration with the Angular frontend and can scale to support multiple concurrent Lean Coffee sessions.
