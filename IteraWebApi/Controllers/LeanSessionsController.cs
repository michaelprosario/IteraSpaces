using Microsoft.AspNetCore.Mvc;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Services;
using System.Threading.Tasks;

namespace IteraWebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeanSessionsController : ControllerBase
{
    private readonly LeanSessionService _sessionService;
    private readonly LeanSessionQueryService _queryService;

    public LeanSessionsController(
        LeanSessionService sessionService,
        LeanSessionQueryService queryService)
    {
        _sessionService = sessionService;
        _queryService = queryService;
    }

    /// <summary>
    /// Store a lean session (create or update)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> StoreSession([FromBody] LeanSession session)
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
    [HttpGet("{sessionId}")]
    public async Task<IActionResult> GetSessionById(string sessionId)
    {
        var query = new GetEntityByIdQuery(sessionId);
        var result = await _sessionService.GetEntityByIdAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get detailed session with topics, votes, and users
    /// </summary>
    [HttpGet("{sessionId}/details")]
    public async Task<IActionResult> GetSessionDetails(string sessionId)
    {
        var query = new GetLeanSessionQuery { SessionId = sessionId };
        var result = await _queryService.GetLeanSessionAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Search sessions with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> SearchSessions(
        [FromQuery] string? searchTerm,
        [FromQuery] SessionStatus? status,
        [FromQuery] string? facilitatorUserId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetLeanSessionsQuery
        {
            SearchTerm = searchTerm ?? string.Empty,
            Status = status,
            FacilitatorUserId = facilitatorUserId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _queryService.GetLeanSessionsAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Close a session
    /// </summary>
    [HttpPost("{sessionId}/close")]
    public async Task<IActionResult> CloseSession(string sessionId)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var command = new CloseSessionCommand
        {
            SessionId = sessionId,
            UserId = userId
        };

        var result = await _sessionService.CloseSessionAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Add a note to a session
    /// </summary>
    [HttpPost("{sessionId}/notes")]
    public async Task<IActionResult> StoreNote(string sessionId, [FromBody] StoreLeanSessionNoteCommand command)
    {
        command.LeanSessionId = sessionId;
        var result = await _sessionService.StoreNoteAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a session (soft delete)
    /// </summary>
    [HttpDelete("{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var command = new DeleteEntityCommand(sessionId)
        {
            UserId = userId
        };

        var result = await _sessionService.DeleteEntityAsync(command);
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
