using System;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengePhaseService : EntityService<ChallengePhase>
{
    private readonly IChallengePhaseRepository _phaseRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengePostRepository _postRepository;

    public ChallengePhaseService(
        IChallengePhaseRepository phaseRepository,
        IChallengeRepository challengeRepository,
        IChallengePostRepository postRepository)
        : base(phaseRepository)
    {
        _phaseRepository = phaseRepository;
        _challengeRepository = challengeRepository;
        _postRepository = postRepository;
    }

    /// <summary>
    /// Custom StoreEntityAsync to validate challenge exists and check date overlaps
    /// </summary>
    public new async Task<AppResult<ChallengePhase>> StoreEntityAsync(StoreEntityCommand<ChallengePhase> command)
    {
        // Validate that challenge exists
        var challengeExists = await _challengeRepository.RecordExists(command.Entity.ChallengeId);
        if (!challengeExists)
        {
            return AppResult<ChallengePhase>.FailureResult(
                "Challenge not found",
                "CHALLENGE_NOT_FOUND");
        }

        // Check for overlapping date ranges if dates are specified
        if (command.Entity.StartDate.HasValue && command.Entity.EndDate.HasValue)
        {
            var phasesInChallenge = await _phaseRepository.GetByChallengeIdAsync(command.Entity.ChallengeId);
            var overlappingPhases = phasesInChallenge.Where(p => 
                p.Id != command.Entity.Id && // Exclude current phase if updating
                !p.IsDeleted &&
                p.StartDate.HasValue && 
                p.EndDate.HasValue &&
                // Check for overlap
                p.StartDate < command.Entity.EndDate && 
                p.EndDate > command.Entity.StartDate).ToList();

            if (overlappingPhases.Any())
            {
                return AppResult<ChallengePhase>.FailureResult(
                    $"Phase dates overlap with existing phase: {overlappingPhases.First().Name}",
                    "OVERLAPPING_DATES");
            }
        }

        return await base.StoreEntityAsync(command);
    }

    /// <summary>
    /// Custom DeleteEntityAsync to prevent deletion if phase has posts
    /// </summary>
    public new async Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)
    {
        var postCount = await _postRepository.GetPostCountByPhaseIdAsync(command.EntityId);
        if (postCount > 0)
        {
            return AppResult<bool>.FailureResult(
                $"Cannot delete phase with {postCount} post(s). Please archive the phase instead.",
                "HAS_POSTS");
        }

        return await base.DeleteEntityAsync(command);
    }

    /// <summary>
    /// Update phase status
    /// </summary>
    public async Task<AppResult<ChallengePhase>> UpdateStatusAsync(UpdateChallengePhaseStatusCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ChallengePhaseId))
        {
            return AppResult<ChallengePhase>.FailureResult(
                "ChallengePhaseId is required",
                "INVALID_PHASE_ID");
        }

        var phase = await _phaseRepository.GetById(command.ChallengePhaseId);
        if (phase == null)
        {
            return AppResult<ChallengePhase>.FailureResult(
                "Phase not found",
                "PHASE_NOT_FOUND");
        }

        // Update status
        phase.Status = command.Status;
        phase.UpdatedAt = DateTime.UtcNow;
        phase.UpdatedBy = command.UserId;

        await _phaseRepository.Update(phase);

        return AppResult<ChallengePhase>.SuccessResult(
            phase,
            "Phase status updated successfully");
    }
}
