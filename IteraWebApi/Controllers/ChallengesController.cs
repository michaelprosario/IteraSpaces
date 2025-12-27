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
public class ChallengesController : ControllerBase
{
    private readonly ChallengeService _challengeService;
    private readonly ChallengeQueryService _queryService;
    private readonly ILogger<ChallengesController> _logger;

    public ChallengesController(
        ChallengeService challengeService,
        ChallengeQueryService queryService,
        ILogger<ChallengesController> logger)
    {
        _challengeService = challengeService;
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// List challenges with optional filtering
    /// </summary>
    [HttpPost("list")]
    public async Task<IActionResult> ListChallenges([FromBody] GetChallengesQuery query)
    {
        var result = await _queryService.GetChallengesAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get challenge details with phases and stats
    /// </summary>
    [HttpPost("get")]
    public async Task<IActionResult> GetChallenge([FromBody] GetChallengeQuery query)
    {
        var result = await _queryService.GetChallengeAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Store a challenge (create or update)
    /// </summary>
    [HttpPost("store")]
    public async Task<IActionResult> StoreChallenge([FromBody] Challenge challenge)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new ChallengeValidator();
        var validationResult = validator.Validate(challenge);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<Challenge>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<Challenge>(challenge)
        {
            UserId = userId
        };

        var result = await _challengeService.StoreEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update challenge status
    /// </summary>
    [HttpPost("updatestatus")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateChallengeStatusCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _challengeService.UpdateStatusAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a challenge (soft delete)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeleteChallenge([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _challengeService.DeleteEntityAsync(command);
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
            "ENTITY_NOT_FOUND" or "CHALLENGE_NOT_FOUND" => NotFound(result),
            "ENTITY_EXISTS" or "HAS_ACTIVE_PHASES" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
