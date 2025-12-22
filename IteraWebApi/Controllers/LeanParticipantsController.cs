using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using AppCore.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeanParticipantsController : ControllerBase
{
    private readonly LeanParticipantService _participantService;
    private readonly ILeanCoffeeNotificationService _notificationService;

    public LeanParticipantsController(
        LeanParticipantService participantService,
        ILeanCoffeeNotificationService notificationService)
    {
        _participantService = participantService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Add a participant to a session
    /// </summary>
    [HttpPost("AddParticipantAsync")]
    public async Task<IActionResult> AddParticipantAsync([FromBody] LeanParticipant participant)
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
    [HttpPost("GetEntityByIdAsync")]
    public async Task<IActionResult> GetEntityByIdAsync([FromBody] GetEntityByIdQuery query)
    {
        var result = await _participantService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove a participant from a session (soft delete)
    /// </summary>
    [HttpPost("DeleteEntityAsync")]
    public async Task<IActionResult> DeleteEntityAsync([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _participantService.DeleteEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Join a session
    /// </summary>
    [HttpPost("JoinSession")]
    public async Task<IActionResult> JoinSession([FromBody] JoinSessionCommand command)
    {
        var result = await _participantService.JoinSessionAsync(
            command.SessionId, 
            command.UserId, 
            command.Role);
        
        if (result.Success && result.Data != null)
        {
            await _notificationService.NotifyParticipantJoinedAsync(
                command.SessionId, 
                command.UserId);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Leave a session
    /// </summary>
    [HttpPost("LeaveSession")]
    public async Task<IActionResult> LeaveSession([FromBody] LeaveSessionCommand command)
    {
        var result = await _participantService.LeaveSessionAsync(
            command.SessionId, 
            command.UserId);
        
        if (result.Success && result.Data != null)
        {
            await _notificationService.NotifyParticipantLeftAsync(
                command.SessionId, 
                command.UserId);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get active participants for a session
    /// </summary>
    [HttpPost("GetActiveParticipants")]
    public async Task<IActionResult> GetActiveParticipants([FromBody] GetActiveParticipantsQuery query)
    {
        var result = await _participantService.GetActiveParticipantsAsync(query.SessionId);
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
