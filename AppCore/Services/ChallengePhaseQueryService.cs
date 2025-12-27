using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengePhaseQueryService
{
    private readonly IChallengePhaseRepository _phaseRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengePostRepository _postRepository;

    public ChallengePhaseQueryService(
        IChallengePhaseRepository phaseRepository,
        IChallengeRepository challengeRepository,
        IChallengePostRepository postRepository)
    {
        _phaseRepository = phaseRepository;
        _challengeRepository = challengeRepository;
        _postRepository = postRepository;
    }

    /// <summary>
    /// Get challenge phases with optional filtering
    /// </summary>
    public async Task<List<ChallengePhase>> GetChallengePhasesAsync(GetChallengePhasesQuery query)
    {
        return await _phaseRepository.SearchAsync(query);
    }

    /// <summary>
    /// Get challenge phase with related data (challenge and post count)
    /// </summary>
    public async Task<AppResult<GetChallengePhaseResult>> GetChallengePhaseAsync(GetChallengePhaseQuery query)
    {
        if (string.IsNullOrWhiteSpace(query.ChallengePhaseId))
        {
            return AppResult<GetChallengePhaseResult>.FailureResult(
                "ChallengePhaseId is required",
                "INVALID_PHASE_ID");
        }

        var phase = await _phaseRepository.GetById(query.ChallengePhaseId);
        if (phase == null)
        {
            return AppResult<GetChallengePhaseResult>.FailureResult(
                "Phase not found",
                "PHASE_NOT_FOUND");
        }

        var challenge = await _challengeRepository.GetById(phase.ChallengeId);
        if (challenge == null)
        {
            return AppResult<GetChallengePhaseResult>.FailureResult(
                "Challenge not found",
                "CHALLENGE_NOT_FOUND");
        }

        var postCount = await _postRepository.GetPostCountByPhaseIdAsync(query.ChallengePhaseId);

        var result = new GetChallengePhaseResult
        {
            Phase = phase,
            Challenge = challenge,
            PostCount = postCount
        };

        return AppResult<GetChallengePhaseResult>.SuccessResult(
            result,
            "Phase retrieved successfully");
    }
}
