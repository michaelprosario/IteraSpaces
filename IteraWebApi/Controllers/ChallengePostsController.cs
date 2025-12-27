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
public class ChallengePostsController : ControllerBase
{
    private readonly ChallengePostService _postService;
    private readonly ChallengePostQueryService _queryService;
    private readonly ILogger<ChallengePostsController> _logger;

    public ChallengePostsController(
        ChallengePostService postService,
        ChallengePostQueryService queryService,
        ILogger<ChallengePostsController> logger)
    {
        _postService = postService;
        _queryService = queryService;
        _logger = logger;
    }

    /// <summary>
    /// List challenge posts with filtering and sorting
    /// </summary>
    [HttpPost("list")]
    public async Task<IActionResult> ListPosts([FromBody] GetChallengePostsQuery query)
    {
        var result = await _queryService.GetChallengePostsAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get challenge post details with comments
    /// </summary>
    [HttpPost("get")]
    public async Task<IActionResult> GetPost([FromBody] GetChallengePostQuery query)
    {
        var result = await _queryService.GetChallengePostAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Store a challenge post (create or update)
    /// </summary>
    [HttpPost("store")]
    public async Task<IActionResult> StorePost([FromBody] ChallengePost post)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new ChallengePostValidator();
        var validationResult = validator.Validate(post);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<ChallengePost>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<ChallengePost>(post)
        {
            UserId = userId
        };

        var result = await _postService.StoreEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a challenge post (soft delete)
    /// </summary>
    [HttpPost("delete")]
    public async Task<IActionResult> DeletePost([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _postService.DeleteEntityAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Vote for a challenge post
    /// </summary>
    [HttpPost("vote")]
    public async Task<IActionResult> Vote([FromBody] VoteChallengePostCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _postService.VoteAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Remove vote from a challenge post
    /// </summary>
    [HttpPost("removevote")]
    public async Task<IActionResult> RemoveVote([FromBody] RemoveVoteChallengePostCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _postService.RemoveVoteAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// List comments for a challenge post
    /// </summary>
    [HttpPost("listcomments")]
    public async Task<IActionResult> ListComments([FromBody] GetChallengePostCommentsQuery query)
    {
        var result = await _queryService.GetCommentsAsync(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Store a comment (create or update)
    /// </summary>
    [HttpPost("storecomment")]
    public async Task<IActionResult> StoreComment([FromBody] ChallengePostComment comment)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context

        var validator = new ChallengePostCommentValidator();
        var validationResult = validator.Validate(comment);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ValidationError
                {
                    PropertyName = e.PropertyName,
                    ErrorMessage = e.ErrorMessage
                })
                .ToList();

            return BadRequest(new AppResult<ChallengePostComment>
            {
                Success = false,
                ErrorCode = "VALIDATION_ERROR",
                ValidationErrors = errors,
                Message = "Validation failed"
            });
        }

        var command = new StoreEntityCommand<ChallengePostComment>(comment)
        {
            UserId = userId
        };

        var result = await _postService.StoreCommentAsync(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a comment (soft delete)
    /// </summary>
    [HttpPost("deletecomment")]
    public async Task<IActionResult> DeleteComment([FromBody] DeleteEntityCommand command)
    {
        var userId = "SYSTEM"; // TODO: Get from auth context
        command.UserId = userId;

        var result = await _postService.DeleteCommentAsync(command);
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
            "ENTITY_NOT_FOUND" or "POST_NOT_FOUND" or "PHASE_NOT_FOUND" or "COMMENT_NOT_FOUND" => NotFound(result),
            "ENTITY_EXISTS" or "VOTE_ALREADY_EXISTS" or "CANNOT_VOTE_OWN_POST" or "PHASE_NOT_OPEN" or "VOTE_NOT_FOUND" => Conflict(result),
            _ => StatusCode(500, result)
        };
    }
}
