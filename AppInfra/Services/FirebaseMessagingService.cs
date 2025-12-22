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
    
    public async Task<FcmBatchResponse> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
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
            
            return new FcmBatchResponse
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
