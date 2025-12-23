using AppCore.Entities;
using AppCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace AppInfra.Services;

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
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send session created notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifySessionUpdatedAsync(string sessionId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "session_updated" },
                { "sessionId", sessionId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for session updated: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send session updated notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifySessionClosedAsync(string sessionId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "session_closed" },
                { "sessionId", sessionId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for session closed: {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send session closed notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifySessionStateChangedAsync(string sessionId, SessionStatus newStatus)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "session_state_changed" },
                { "sessionId", sessionId },
                { "newStatus", newStatus.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for session state changed: {SessionId} to {Status}", sessionId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send session state changed notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifyParticipantJoinedAsync(string sessionId, string userId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "participant_joined" },
                { "sessionId", sessionId },
                { "userId", userId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for participant joined: {UserId} in session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send participant joined notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifyParticipantLeftAsync(string sessionId, string userId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "participant_left" },
                { "sessionId", sessionId },
                { "userId", userId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for participant left: {UserId} in session {SessionId}", userId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send participant left notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifyTopicAddedAsync(string sessionId, string topicId)
    {
        try
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
            
            _logger.LogInformation("Sent FCM notification for topic added: {TopicId} in session {SessionId}", topicId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send topic added notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyTopicUpdatedAsync(string sessionId, string topicId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "topic_updated" },
                { "sessionId", sessionId },
                { "topicId", topicId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for topic updated: {TopicId} in session {SessionId}", topicId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send topic updated notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyTopicDeletedAsync(string sessionId, string topicId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "topic_deleted" },
                { "sessionId", sessionId },
                { "topicId", topicId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for topic deleted: {TopicId} in session {SessionId}", topicId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send topic deleted notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyTopicStatusChangedAsync(string sessionId, string topicId, TopicStatus newStatus)
    {
        try
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
            
            _logger.LogInformation("Sent FCM notification for topic status changed: {TopicId} to {Status}", topicId, newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send topic status changed notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyCurrentTopicChangedAsync(string sessionId, string? topicId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "current_topic_changed" },
                { "sessionId", sessionId },
                { "topicId", topicId ?? "" },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for current topic changed in session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send current topic changed notification for session {SessionId}", sessionId);
        }
    }
    
    public async Task NotifyVoteCastAsync(string sessionId, string topicId, string userId)
    {
        try
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
            
            _logger.LogInformation("Sent FCM notification for vote cast on topic {TopicId}", topicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send vote cast notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyVoteRemovedAsync(string sessionId, string topicId, string userId)
    {
        try
        {
            var topic = await _topicRepository.GetById(topicId);
            if (topic == null) return;
            
            var data = new Dictionary<string, string>
            {
                { "eventType", "vote_removed" },
                { "sessionId", sessionId },
                { "topicId", topicId },
                { "userId", userId },
                { "voteCount", topic.VoteCount.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for vote removed from topic {TopicId}", topicId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send vote removed notification for topic {TopicId}", topicId);
        }
    }
    
    public async Task NotifyNoteAddedAsync(string sessionId, string noteId)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "eventType", "note_added" },
                { "sessionId", sessionId },
                { "noteId", noteId },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };
            
            await _fcmService.SendDataToTopicAsync($"session_{sessionId}", data);
            
            _logger.LogInformation("Sent FCM notification for note added: {NoteId} in session {SessionId}", noteId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send note added notification for session {SessionId}", sessionId);
        }
    }
}
