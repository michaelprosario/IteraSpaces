using System;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengeService : EntityService<Challenge>
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengePhaseRepository _phaseRepository;

    public ChallengeService(
        IChallengeRepository challengeRepository,
        IChallengePhaseRepository phaseRepository) 
        : base(challengeRepository)
    {
        _challengeRepository = challengeRepository;
        _phaseRepository = phaseRepository;
    }

    /// <summary>
    /// Custom DeleteEntityAsync to prevent deletion if challenge has active phases
    /// </summary>
    public new async Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)
    {
        // Get phases for this challenge
        var phases = await _phaseRepository.GetByChallengeIdAsync(command.EntityId);
        var activePhases = phases.Where(p => !p.IsDeleted && 
            (p.Status == ChallengePhaseStatus.Open || p.Status == ChallengePhaseStatus.Planned)).ToList();

        if (activePhases.Any())
        {
            return AppResult<bool>.FailureResult(
                $"Cannot delete challenge with {activePhases.Count} active phase(s). Please archive or delete the phases first.",
                "HAS_ACTIVE_PHASES");
        }

        return await base.DeleteEntityAsync(command);
    }

    /// <summary>
    /// Update challenge status
    /// </summary>
    public async Task<AppResult<Challenge>> UpdateStatusAsync(UpdateChallengeStatusCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ChallengeId))
        {
            return AppResult<Challenge>.FailureResult(
                "ChallengeId is required",
                "INVALID_CHALLENGE_ID");
        }

        var challenge = await _challengeRepository.GetById(command.ChallengeId);
        if (challenge == null)
        {
            return AppResult<Challenge>.FailureResult(
                "Challenge not found",
                "CHALLENGE_NOT_FOUND");
        }

        // Validate status transition
        var validTransition = ValidateStatusTransition(challenge.Status, command.Status);
        if (!validTransition.IsValid)
        {
            return AppResult<Challenge>.FailureResult(
                validTransition.ErrorMessage,
                "INVALID_STATUS_TRANSITION");
        }

        // Update status
        challenge.Status = command.Status;
        challenge.UpdatedAt = DateTime.UtcNow;
        challenge.UpdatedBy = command.UserId;

        await _challengeRepository.Update(challenge);

        return AppResult<Challenge>.SuccessResult(
            challenge,
            "Challenge status updated successfully");
    }

    private (bool IsValid, string ErrorMessage) ValidateStatusTransition(
        ChallengeStatus currentStatus,
        ChallengeStatus newStatus)
    {
        // Allow any transition from Draft
        if (currentStatus == ChallengeStatus.Draft)
        {
            return (true, string.Empty);
        }

        // Cannot go back to Draft from other states
        if (newStatus == ChallengeStatus.Draft && currentStatus != ChallengeStatus.Draft)
        {
            return (false, "Cannot change status back to Draft");
        }

        // Can go from Open to Closed or Archived
        if (currentStatus == ChallengeStatus.Open && 
            (newStatus == ChallengeStatus.Closed || newStatus == ChallengeStatus.Archived))
        {
            return (true, string.Empty);
        }

        // Can go from Closed to Archived or back to Open
        if (currentStatus == ChallengeStatus.Closed && 
            (newStatus == ChallengeStatus.Open || newStatus == ChallengeStatus.Archived))
        {
            return (true, string.Empty);
        }

        // Cannot transition from Archived to any other state
        if (currentStatus == ChallengeStatus.Archived)
        {
            return (false, "Cannot change status from Archived");
        }

        return (false, $"Invalid status transition from {currentStatus} to {newStatus}");
    }
}
