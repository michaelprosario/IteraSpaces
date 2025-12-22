using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using IteraWebApi.Hubs;
using System.Linq;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

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

    /// <summary>
    /// Store a topic (create or update)
    /// </summary>
    [HttpPost("StoreEntityAsync")]
    public async Task<IActionResult> StoreEntityAsync([FromBody] LeanTopic topic)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new LeanTopicValidator();
        var validationResult = validator.Validate(topic);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<LeanTopic>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<LeanTopic>(topic)
        {
            UserId = userId
        };

        var result = await _topicService.StoreEntityAsync(command);
        
        if (result.Success && result.Data != null)
        {
            // Notify all session participants via SignalR
            await _hubContext.Clients.Group($"session_{result.Data.LeanSessionId}")
                .SendAsync("TopicAdded", new 
                { 
                    sessionId = result.Data.LeanSessionId,
                    topicId = result.Data.Id,
                    topic = result.Data,
                    timestamp = System.DateTime.UtcNow 
                });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get topic by ID
    /// </summary>
    [HttpPost("GetEntityByIdAsync")]
    public async Task<IActionResult> GetEntityByIdAsync([FromBody] GetEntityByIdQuery query)
    {
        var result = await _topicService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Vote for a topic
    /// </summary>
    [HttpPost("VoteForLeanTopicAsync")]
    public async Task<IActionResult> VoteForLeanTopicAsync([FromBody] VoteForLeanTopicCommand command)
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
                        timestamp = System.DateTime.UtcNow 
                    });
            }
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Remove vote from a topic
    /// </summary>
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
                    timestamp = System.DateTime.UtcNow 
                });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Set topic status
    /// </summary>
    [HttpPost("SetTopicStatusAsync")]
    public async Task<IActionResult> SetTopicStatusAsync([FromBody] SetTopicStatusCommand command)
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
                    timestamp = System.DateTime.UtcNow 
                });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a topic (soft delete)
    /// </summary>
    [HttpPost("DeleteEntityAsync")]
    public async Task<IActionResult> DeleteEntityAsync([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _topicService.DeleteEntityAsync(command);
        return HandleResult(result);
    }

    private IActionResult HandleResult<T>(AppResult<T> result)
    {
        if (result.Success)
        {
            return Ok(result);
        }

        return result.ErrorCode switch
        {
            "VALIDATION_ERROR" => BadRequest(result),
            "ENTITY_NOT_FOUND" or "TOPIC_NOT_FOUND" or "SESSION_NOT_FOUND" => NotFound(result),
            "ENTITY_EXISTS" or "VOTE_ALREADY_EXISTS" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
