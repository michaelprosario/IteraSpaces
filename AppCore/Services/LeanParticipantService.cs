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
}
