using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengeQueryService
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengePhaseRepository _phaseRepository;
    private readonly IChallengePostRepository _postRepository;

    public ChallengeQueryService(
        IChallengeRepository challengeRepository,
        IChallengePhaseRepository phaseRepository,
        IChallengePostRepository postRepository)
    {
        _challengeRepository = challengeRepository;
        _phaseRepository = phaseRepository;
        _postRepository = postRepository;
    }

    /// <summary>
    /// Search challenges with filtering and pagination
    /// </summary>
    public async Task<PagedResults<Challenge>> GetChallengesAsync(GetChallengesQuery query)
    {
        return await _challengeRepository.SearchAsync(query);
    }

    /// <summary>
    /// Get challenge with related data (phases and post count)
    /// </summary>
    public async Task<AppResult<GetChallengeResult>> GetChallengeAsync(GetChallengeQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.ChallengeId))
        {
            return AppResult<GetChallengeResult>.FailureResult(
                "ChallengeId is required",
                "INVALID_CHALLENGE_ID");
        }

        var challenge = await _challengeRepository.GetById(query.ChallengeId);
        if (challenge == null)
        {
            return AppResult<GetChallengeResult>.FailureResult(
                "Challenge not found",
                "CHALLENGE_NOT_FOUND");
        }

        var phases = await _phaseRepository.GetByChallengeIdAsync(query.ChallengeId);
        var totalPosts = await _postRepository.GetPostCountByChallengeIdAsync(query.ChallengeId);

        var result = new GetChallengeResult
        {
            Challenge = challenge,
            Phases = phases,
            TotalPosts = totalPosts
        };

        return AppResult<GetChallengeResult>.SuccessResult(
            result,
            "Challenge retrieved successfully");
    }
}
