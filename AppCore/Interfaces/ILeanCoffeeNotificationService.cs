using AppCore.Entities;

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
    Task NotifyTopicDeletedAsync(string sessionId, string topicId);
    Task NotifyTopicStatusChangedAsync(string sessionId, string topicId, TopicStatus newStatus);
    Task NotifyCurrentTopicChangedAsync(string sessionId, string? topicId);
    
    // Vote Events
    Task NotifyVoteCastAsync(string sessionId, string topicId, string userId);
    Task NotifyVoteRemovedAsync(string sessionId, string topicId, string userId);
    
    // Note Events
    Task NotifyNoteAddedAsync(string sessionId, string noteId);
}
