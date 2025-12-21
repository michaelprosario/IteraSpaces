using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.Entities;
using AppCore.Services;
using System.Linq;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeanParticipantsController : ControllerBase
{
    private readonly LeanParticipantService _participantService;

    public LeanParticipantsController(LeanParticipantService participantService)
    {
        _participantService = participantService;
    }

    /// <summary>
    /// Add a participant to a session
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddParticipant([FromBody] LeanParticipant participant)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new LeanParticipantValidator();
        var validationResult = validator.Validate(participant);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<LeanParticipant>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new AddEntityCommand<LeanParticipant>(participant)
        {
            UserId = userId
        };

        var result = await _participantService.AddParticipantAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get participant by ID
    /// </summary>
    [HttpGet("{participantId}")]
    public async Task<IActionResult> GetParticipantById(string participantId)
    {
        var query = new GetEntityByIdQuery(participantId);
        var result = await _participantService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove a participant from a session (soft delete)
    /// </summary>
    [HttpDelete("{participantId}")]
    public async Task<IActionResult> RemoveParticipant(string participantId)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var command = new DeleteEntityCommand(participantId)
        {
            UserId = userId
        };

        var result = await _participantService.DeleteEntityAsync(command);
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
            "ENTITY_NOT_FOUND" or "SESSION_NOT_FOUND" => NotFound(result),
            "ENTITY_EXISTS" or "PARTICIPANT_ALREADY_EXISTS" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
