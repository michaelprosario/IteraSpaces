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
public class ChallengePhasesController : ControllerBase
{
    private readonly ChallengePhaseService _phaseService;
    private readonly ChallengePhaseQueryService _queryService;
    private readonly ILogger<ChallengePhasesController> _logger;

    public ChallengePhasesController(
        ChallengePhaseService phaseService,
        ChallengePhaseQueryService queryService,
        ILogger<ChallengePhasesController> logger)
    {
        _phaseService = phaseService;
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// List challenge phases with optional filtering
    /// </summary>
    [HttpPost("list")]
    public async Task<IActionResult> ListPhases([FromBody] GetChallengePhasesQuery query)
    {
        var result = await _queryService.GetChallengePhasesAsync(query);
        var appResult = new AppResult<List<ChallengePhase>>
        {
            Success = true,
            Data = result,
            Message = "Challenge phases retrieved successfully"
        };
        return HandleResult(appResult);
    }

    /// <summary>
    /// Get challenge phase details
    /// </summary>
    [HttpPost("get")]
    public async Task<IActionResult> GetPhase([FromBody] GetChallengePhaseQuery query)
    {
        var result = await _queryService.GetChallengePhaseAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Store a challenge phase (create or update)
    /// </summary>
    [HttpPost("store")]
    public async Task<IActionResult> StorePhase([FromBody] ChallengePhase phase)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new ChallengePhaseValidator();
        var validationResult = validator.Validate(phase);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<ChallengePhase>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<ChallengePhase>(phase)
        {
            UserId = userId
        };

        var result = await _phaseService.StoreEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update challenge phase status
    /// </summary>
    [HttpPost("updatestatus")]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateChallengePhaseStatusCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _phaseService.UpdateStatusAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a challenge phase (soft delete)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeletePhase([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _phaseService.DeleteEntityAsync(command);
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
            "ENTITY_NOT_FOUND" or "PHASE_NOT_FOUND" or "CHALLENGE_NOT_FOUND" => NotFound(result),
            "ENTITY_EXISTS" or "HAS_POSTS" or "INVALID_CHALLENGE_ID" or "OVERLAPPING_DATES" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
