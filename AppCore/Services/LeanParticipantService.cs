using System;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class LeanParticipantService : EntityService<LeanParticipant>
{
    private readonly ILeanParticipantRepository _participantRepository;
    private readonly ILeanSessionRepository _sessionRepository;

    public LeanParticipantService(
        ILeanParticipantRepository participantRepository,
        ILeanSessionRepository sessionRepository) : base(participantRepository)
    {
        _participantRepository = participantRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AppResult<LeanParticipant>> AddParticipantAsync(AddEntityCommand<LeanParticipant> command)
    {
        // Check if session exists
        var session = await _sessionRepository.GetById(command.Entity.LeanSessionId);
        if (session == null)
        {
            return AppResult<LeanParticipant>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        // Check if participant already exists
        var existingParticipant = await _participantRepository.GetBySessionAndUserIdAsync(
            command.Entity.LeanSessionId,
            command.Entity.UserId);

        if (existingParticipant != null && existingParticipant.IsActive)
        {
            return AppResult<LeanParticipant>.FailureResult(
                "User is already an active participant in this session",
                "PARTICIPANT_ALREADY_EXISTS");
        }

        // Set joined timestamp
        command.Entity.JoinedAt = DateTime.UtcNow;
        command.Entity.IsActive = true;

        return await base.AddEntityAsync(command);
    }

    public async Task<AppResult<LeanParticipant>> JoinSessionAsync(string sessionId, string userId, ParticipantRole role)
    {
        // Check if session exists
        var session = await _sessionRepository.GetById(sessionId);
        if (session == null)
        {
            return AppResult<LeanParticipant>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        // Check if already an active participant
        var existingParticipant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
        if (existingParticipant != null)
        {
            // Reactivate if previously left
            if (!existingParticipant.IsActive)
            {
                existingParticipant.IsActive = true;
                existingParticipant.LeftAt = null;
                existingParticipant.UpdatedAt = DateTime.UtcNow;
                await _participantRepository.Update(existingParticipant);
            }
            return AppResult<LeanParticipant>.SuccessResult(existingParticipant);
        }

        // Create new participant
        var participant = new LeanParticipant
        {
            Id = Guid.NewGuid().ToString(),
            LeanSessionId = sessionId,
            UserId = userId,
            Role = role,
            JoinedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        await _participantRepository.Add(participant);
        
        return AppResult<LeanParticipant>.SuccessResult(
            participant,
            "Participant joined successfully");
    }

    public async Task<AppResult<LeanParticipant>> LeaveSessionAsync(string sessionId, string userId)
    {
        var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
        if (participant == null || !participant.IsActive)
        {
            return AppResult<LeanParticipant>.FailureResult(
                "Active participant not found",
                "PARTICIPANT_NOT_FOUND");
        }

        participant.IsActive = false;
        participant.LeftAt = DateTime.UtcNow;
        participant.UpdatedAt = DateTime.UtcNow;
        await _participantRepository.Update(participant);

        return AppResult<LeanParticipant>.SuccessResult(
            participant,
            "Participant left session");
    }

    public async Task<AppResult<IEnumerable<LeanParticipant>>> GetActiveParticipantsAsync(string sessionId)
    {
        var participants = await _participantRepository.GetActiveParticipantsBySessionAsync(sessionId);
        return AppResult<IEnumerable<LeanParticipant>>.SuccessResult(participants);
    }

    public async Task<AppResult<bool>> IsUserInSessionAsync(string sessionId, string userId)
    {
        var participant = await _participantRepository.GetBySessionAndUserIdAsync(sessionId, userId);
        return AppResult<bool>.SuccessResult(participant != null && participant.IsActive);
    }
}
