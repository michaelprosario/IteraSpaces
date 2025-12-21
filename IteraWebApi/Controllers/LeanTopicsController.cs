using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using System.Linq;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeanTopicsController : ControllerBase
{
    private readonly LeanTopicService _topicService;

    public LeanTopicsController(LeanTopicService topicService)
    {
        _topicService = topicService;
    }

    /// <summary>
    /// Store a topic (create or update)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> StoreTopic([FromBody] LeanTopic topic)
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
        return HandleResult(result);
    }

    /// <summary>
    /// Get topic by ID
    /// </summary>
    [HttpGet("{topicId}")]
    public async Task<IActionResult> GetTopicById(string topicId)
    {
        var query = new GetEntityByIdQuery(topicId);
        var result = await _topicService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Vote for a topic
    /// </summary>
    [HttpPost("{topicId}/vote")]
    public async Task<IActionResult> VoteForTopic(string topicId, [FromBody] VoteForLeanTopicCommand command)
    {
        command.LeanTopicId = topicId;
        var result = await _topicService.VoteForLeanTopicAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Set topic status
    /// </summary>
    [HttpPut("{topicId}/status")]
    public async Task<IActionResult> SetTopicStatus(string topicId, [FromBody] SetTopicStatusCommand command)
    {
        command.TopicId = topicId;
        var result = await _topicService.SetTopicStatusAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a topic (soft delete)
    /// </summary>
    [HttpDelete("{topicId}")]
    public async Task<IActionResult> DeleteTopic(string topicId)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var command = new DeleteEntityCommand(topicId)
        {
            UserId = userId
        };

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
