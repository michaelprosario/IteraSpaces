using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using AppCore.Interfaces;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeanSessionsController : ControllerBase
{
    private readonly LeanSessionService _sessionService;
    private readonly LeanSessionQueryService _queryService;
    private readonly ILeanCoffeeNotificationService _notificationService;

    public LeanSessionsController(
        LeanSessionService sessionService,
        LeanSessionQueryService queryService,
        ILeanCoffeeNotificationService notificationService)
    {
        _sessionService = sessionService;
        _queryService = queryService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Store a lean session (create or update)
    /// </summary>
    [HttpPost("StoreEntityAsync")]
    public async Task<IActionResult> StoreEntityAsync([FromBody] LeanSession session)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new LeanSessionValidator();
        var validationResult = validator.Validate(session);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<LeanSession>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<LeanSession>(session)
        {
            UserId = userId
        };

        var result = await _sessionService.StoreEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Get session by ID
    /// </summary>
    [HttpPost("GetEntityByIdAsync")]
    public async Task<IActionResult> GetEntityByIdAsync([FromBody] GetEntityByIdQuery query)
    {
        var result = await _sessionService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get detailed session with topics, votes, and users
    /// </summary>
    [HttpPost("GetLeanSessionAsync")]
    public async Task<IActionResult> GetLeanSessionAsync([FromBody] GetLeanSessionQuery query)
    {
        var result = await _queryService.GetLeanSessionAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Search sessions with filters
    /// </summary>
    [HttpPost("GetLeanSessionsAsync")]
    public async Task<IActionResult> GetLeanSessionsAsync([FromBody] GetLeanSessionsQuery query)
    {
        var result = await _queryService.GetLeanSessionsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Close a session
    /// </summary>
    [HttpPost("CloseSessionAsync")]
    public async Task<IActionResult> CloseSessionAsync([FromBody] CloseSessionCommand command)
    {
        var result = await _sessionService.CloseSessionAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Add a note to a session
    /// </summary>
    [HttpPost("StoreNoteAsync")]
    public async Task<IActionResult> StoreNoteAsync([FromBody] StoreLeanSessionNoteCommand command)
    {
        var result = await _sessionService.StoreNoteAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a session (soft delete)
    /// </summary>
    [HttpPost("DeleteEntityAsync")]
    public async Task<IActionResult> DeleteEntityAsync([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _sessionService.DeleteEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Change session status
    /// </summary>
    [HttpPost("ChangeSessionStatus")]
    public async Task<IActionResult> ChangeSessionStatus([FromBody] ChangeSessionStatusCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        var result = await _sessionService.ChangeSessionStatusAsync(
            command.SessionId, 
            command.NewStatus, 
            userId);
        
        if (result.Success && result.Data != null)
        {
            await _notificationService.NotifySessionStateChangedAsync(
                command.SessionId, 
                command.NewStatus);
        }

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
            "ENTITY_EXISTS" or "SESSION_ALREADY_COMPLETED" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
