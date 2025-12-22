using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DeviceTokensController : ControllerBase
{
    private readonly IUserDeviceTokenRepository _tokenRepository;
    private readonly IFirebaseMessagingService _fcmService;
    private readonly ILogger<DeviceTokensController> _logger;
    
    public DeviceTokensController(
        IUserDeviceTokenRepository tokenRepository,
        IFirebaseMessagingService fcmService,
        ILogger<DeviceTokensController> logger)
    {
        _tokenRepository = tokenRepository;
        _fcmService = fcmService;
        _logger = logger;
    }
    
    [HttpPost("RegisterToken")]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenRequest request)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }
            
            // Check if token already exists
            var existingToken = await _tokenRepository.GetByTokenAsync(request.Token);
            if (existingToken != null)
            {
                // Update last used timestamp
                existingToken.LastUsedAt = DateTime.UtcNow;
                existingToken.IsActive = true;
                await _tokenRepository.Update(existingToken);
                
                return Ok(new { success = true, message = "Device token updated" });
            }
            
            // Create new token
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
            
            _logger.LogInformation("Registered device token for user {UserId}", userId);
            
            return Ok(new { success = true, message = "Device token registered" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device token");
            return StatusCode(500, new { success = false, message = "Failed to register device token" });
        }
    }
    
    [HttpPost("SubscribeToSession")]
    public async Task<IActionResult> SubscribeToSession([FromBody] SubscribeToSessionRequest request)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }
            
            var tokens = await _tokenRepository.GetActiveTokensByUserIdAsync(userId);
            
            if (!tokens.Any())
            {
                return BadRequest(new { success = false, message = "No active device tokens found" });
            }
            
            var successCount = 0;
            foreach (var token in tokens)
            {
                var subscribed = await _fcmService.SubscribeToTopicAsync(token.DeviceToken, $"session_{request.SessionId}");
                if (subscribed)
                {
                    successCount++;
                }
            }
            
            _logger.LogInformation("Subscribed {Count} devices to session {SessionId} for user {UserId}", 
                successCount, request.SessionId, userId);
            
            return Ok(new { success = true, subscribedDevices = successCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to subscribe to session {SessionId}", request.SessionId);
            return StatusCode(500, new { success = false, message = "Failed to subscribe to session" });
        }
    }
    
    [HttpPost("UnsubscribeFromSession")]
    public async Task<IActionResult> UnsubscribeFromSession([FromBody] UnsubscribeFromSessionRequest request)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }
            
            var tokens = await _tokenRepository.GetActiveTokensByUserIdAsync(userId);
            
            if (!tokens.Any())
            {
                return Ok(new { success = true, message = "No active device tokens found" });
            }
            
            var successCount = 0;
            foreach (var token in tokens)
            {
                var unsubscribed = await _fcmService.UnsubscribeFromTopicAsync(token.DeviceToken, $"session_{request.SessionId}");
                if (unsubscribed)
                {
                    successCount++;
                }
            }
            
            _logger.LogInformation("Unsubscribed {Count} devices from session {SessionId} for user {UserId}", 
                successCount, request.SessionId, userId);
            
            return Ok(new { success = true, unsubscribedDevices = successCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unsubscribe from session {SessionId}", request.SessionId);
            return StatusCode(500, new { success = false, message = "Failed to unsubscribe from session" });
        }
    }
    
    [HttpDelete("DeactivateToken")]
    public async Task<IActionResult> DeactivateToken([FromQuery] string token)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }
            
            await _tokenRepository.DeactivateTokenAsync(token);
            
            _logger.LogInformation("Deactivated device token for user {UserId}", userId);
            
            return Ok(new { success = true, message = "Device token deactivated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deactivate device token");
            return StatusCode(500, new { success = false, message = "Failed to deactivate device token" });
        }
    }
}
