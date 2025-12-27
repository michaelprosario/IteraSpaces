using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengePostQueryService
{
    private readonly IChallengePostRepository _postRepository;
    private readonly IChallengePhaseRepository _phaseRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengePostVoteRepository _voteRepository;
    private readonly IChallengePostCommentRepository _commentRepository;
    private readonly IUserRepository _userRepository;

    public ChallengePostQueryService(
        IChallengePostRepository postRepository,
        IChallengePhaseRepository phaseRepository,
        IChallengeRepository challengeRepository,
        IChallengePostVoteRepository voteRepository,
        IChallengePostCommentRepository commentRepository,
        IUserRepository userRepository)
    {
        _postRepository = postRepository;
        _phaseRepository = phaseRepository;
        _challengeRepository = challengeRepository;
        _voteRepository = voteRepository;
        _commentRepository = commentRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Search challenge posts with filtering, sorting, and pagination
    /// </summary>
    public async Task<PagedResults<ChallengePost>> GetChallengePostsAsync(GetChallengePostsQuery query)
    {
        return await _postRepository.SearchAsync(query);
    }

    /// <summary>
    /// Get challenge post with full details including comments and vote status
    /// </summary>
    public async Task<AppResult<GetChallengePostResult>> GetChallengePostAsync(GetChallengePostQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.ChallengePostId))
        {
            return AppResult<GetChallengePostResult>.FailureResult(
                "ChallengePostId is required",
                "INVALID_POST_ID");
        }

        var post = await _postRepository.GetById(query.ChallengePostId);
        if (post == null)
        {
            return AppResult<GetChallengePostResult>.FailureResult(
                "Post not found",
                "POST_NOT_FOUND");
        }

        var phase = await _phaseRepository.GetById(post.ChallengePhaseId);
        if (phase == null)
        {
            return AppResult<GetChallengePostResult>.FailureResult(
                "Phase not found",
                "PHASE_NOT_FOUND");
        }

        var challenge = await _challengeRepository.GetById(phase.ChallengeId);
        if (challenge == null)
        {
            return AppResult<GetChallengePostResult>.FailureResult(
                "Challenge not found",
                "CHALLENGE_NOT_FOUND");
        }

        // Get user who submitted the post
        var user = await _userRepository.GetById(post.SubmittedByUserId);
        var username = user?.DisplayName ?? "Unknown User";

        // Check if requesting user has voted
        var hasUserVoted = false;
        if (!string.IsNullOrWhiteSpace(query.RequestingUserId))
        {
            hasUserVoted = await _voteRepository.HasUserVotedAsync(
                query.ChallengePostId,
                query.RequestingUserId);
        }

        // Get comments
        var comments = await _commentRepository.GetByPostIdAsync(query.ChallengePostId);

        var result = new GetChallengePostResult
        {
            Post = post,
            Phase = phase,
            Challenge = challenge,
            SubmittedByUsername = username,
            HasUserVoted = hasUserVoted,
            Comments = comments.Where(c => !c.IsDeleted).ToList()
        };

        return AppResult<GetChallengePostResult>.SuccessResult(
            result,
            "Post retrieved successfully");
    }

    /// <summary>
    /// Get paginated comments for a challenge post
    /// </summary>
    public async Task<PagedResults<ChallengePostComment>> GetCommentsAsync(GetChallengePostCommentsQuery query)
    {
        return await _commentRepository.GetPagedByPostIdAsync(query);
    }
}
