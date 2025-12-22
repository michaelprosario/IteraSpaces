using AppCore.Common;
using AppCore.Entities;
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
    public async Task<AppResult<bool>> JoinSession(string sessionId, string userId)
    {
        try
        {
            // Validate session exists
            var session = await _sessionRepository.GetById(sessionId);
            if (session == null)
                return AppResult<bool>.FailureResult("Session not found");

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
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = userId
                };
                await _participantRepository.Add(newParticipant);
            }
            else
            {
                // Reactivate if they rejoined
                participant.IsActive = true;
                participant.LeftAt = null;
                participant.UpdatedAt = DateTime.UtcNow;
                await _participantRepository.Update(participant);
            }

            // Notify others in the session
            await Clients.OthersInGroup($"session_{sessionId}")
                .SendAsync("ParticipantJoined", new { sessionId, userId, timestamp = DateTime.UtcNow });

            _logger.LogInformation("User {UserId} joined session {SessionId}", userId, sessionId);
            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining session {SessionId}", sessionId);
            return AppResult<bool>.FailureResult($"Failed to join session: {ex.Message}");
        }
    }

    /// <summary>
    /// Leave a lean coffee session
    /// </summary>
    public async Task<AppResult<bool>> LeaveSession(string sessionId, string userId)
    {
        try
        {
            // Update participant status
            var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
            if (participant != null)
            {
                participant.IsActive = false;
                participant.LeftAt = DateTime.UtcNow;
                participant.UpdatedAt = DateTime.UtcNow;
                await _participantRepository.Update(participant);
            }

            // Remove from SignalR group
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session_{sessionId}");

            // Notify others
            await Clients.OthersInGroup($"session_{sessionId}")
                .SendAsync("ParticipantLeft", new { sessionId, userId, timestamp = DateTime.UtcNow });

            _logger.LogInformation("User {UserId} left session {SessionId}", userId, sessionId);
            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving session {SessionId}", sessionId);
            return AppResult<bool>.FailureResult($"Failed to leave session: {ex.Message}");
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
    public async Task<AppResult<bool>> NotifyTopicAdded(string sessionId, string topicId)
    {
        try
        {
            var topic = await _topicRepository.GetById(topicId);
            if (topic == null)
                return AppResult<bool>.FailureResult("Topic not found");

            await Clients.Group($"session_{sessionId}")
                .SendAsync("TopicAdded", new 
                { 
                    sessionId, 
                    topicId, 
                    topic,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic added");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a topic was edited
    /// </summary>
    public async Task<AppResult<bool>> NotifyTopicEdited(string topicId)
    {
        try
        {
            var topic = await _topicRepository.GetById(topicId);
            if (topic == null)
                return AppResult<bool>.FailureResult("Topic not found");

            await Clients.Group($"session_{topic.LeanSessionId}")
                .SendAsync("TopicEdited", new 
                { 
                    topicId, 
                    sessionId = topic.LeanSessionId,
                    topic,
                    timestamp = DateTime.UtcNow 
                });

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic edited");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a topic was deleted
    /// </summary>
    public async Task<AppResult<bool>> NotifyTopicDeleted(string topicId, string sessionId)
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

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic deleted");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a vote was cast
    /// Includes updated vote count
    /// </summary>
    public async Task<AppResult<bool>> NotifyVoteCast(string topicId, string sessionId, int newVoteCount, string userId)
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

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying vote cast");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that a vote was removed
    /// </summary>
    public async Task<AppResult<bool>> NotifyVoteRemoved(string topicId, string sessionId, int newVoteCount, string userId)
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

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying vote removed");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that session status changed
    /// </summary>
    public async Task<AppResult<bool>> NotifySessionStatusChanged(string sessionId, SessionStatus newStatus)
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

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying session status changed");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }

    /// <summary>
    /// Notify all participants that topic status changed
    /// </summary>
    public async Task<AppResult<bool>> NotifyTopicStatusChanged(string topicId, string sessionId, TopicStatus newStatus)
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

            return AppResult<bool>.SuccessResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying topic status changed");
            return AppResult<bool>.FailureResult(ex.Message);
        }
    }
}
