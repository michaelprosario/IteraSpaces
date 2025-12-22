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
    Task<FcmBatchResponse> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
}

public class FcmBatchResponse
{
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<string> FailedTokens { get; set; } = new();
}
