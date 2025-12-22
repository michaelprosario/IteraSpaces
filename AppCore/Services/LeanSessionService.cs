using System;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class LeanSessionService : EntityService<LeanSession>
{
    private readonly ILeanSessionRepository _sessionRepository;
    private readonly ILeanSessionNoteRepository _noteRepository;

    public LeanSessionService(
        ILeanSessionRepository sessionRepository,
        ILeanSessionNoteRepository noteRepository) : base(sessionRepository)
    {
        _sessionRepository = sessionRepository;
        _noteRepository = noteRepository;
    }

    public async Task<AppResult<LeanSessionNote>> StoreNoteAsync(StoreLeanSessionNoteCommand command)
    {
        // Check if session exists
        var session = await _sessionRepository.GetById(command.LeanSessionId);
        if (session == null)
        {
            return AppResult<LeanSessionNote>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        LeanSessionNote note;
        if (string.IsNullOrEmpty(command.Id))
        {
            // Create new note
            note = new LeanSessionNote
            {
                Id = Guid.NewGuid().ToString(),
                LeanSessionId = command.LeanSessionId,
                LeanTopicId = command.LeanTopicId,
                Content = command.Content,
                NoteType = command.NoteType,
                CreatedByUserId = command.CreatedByUserId,
                //AssignedToUserId = command.AssignedToUserId,
                //DueDate = command.DueDate,
                //IsCompleted = command.IsCompleted,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = command.CreatedByUserId
            };
            await _noteRepository.Add(note);
        }
        else
        {
            // Update existing note
            var existingNote = await _noteRepository.GetById(command.Id);
            if (existingNote == null)
            {
                return AppResult<LeanSessionNote>.FailureResult(
                    "Note not found",
                    "NOTE_NOT_FOUND");
            }

            existingNote.Content = command.Content;
            existingNote.NoteType = command.NoteType;
            //existingNote.AssignedToUserId = command.AssignedToUserId;
            ///existingNote.DueDate = command.DueDate;
            //existingNote.IsCompleted = command.IsCompleted;
            existingNote.UpdatedAt = DateTime.UtcNow;
            existingNote.UpdatedBy = command.CreatedByUserId;

            await _noteRepository.Update(existingNote);
            note = existingNote;
        }

        return AppResult<LeanSessionNote>.SuccessResult(
            note,
            "Note saved successfully");
    }

    public async Task<AppResult<LeanSession>> CloseSessionAsync(CloseSessionCommand command)
    {
        var session = await _sessionRepository.GetById(command.SessionId);
        if (session == null)
        {
            return AppResult<LeanSession>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        if (session.Status == SessionStatus.Completed)
        {
            return AppResult<LeanSession>.FailureResult(
                "Session is already completed",
                "SESSION_ALREADY_COMPLETED");
        }

        session.Status = SessionStatus.Completed;
        session.ActualEndTime = DateTime.UtcNow;
        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedBy = command.UserId;

        await _sessionRepository.Update(session);

        return AppResult<LeanSession>.SuccessResult(
            session,
            "Session closed successfully");
    }

    public async Task<AppResult<LeanSession>> ChangeSessionStatusAsync(string sessionId, SessionStatus newStatus, string userId)
    {
        var session = await _sessionRepository.GetById(sessionId);
        if (session == null)
        {
            return AppResult<LeanSession>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        session.Status = newStatus;
        
        // Set timestamps based on status
        if (newStatus == SessionStatus.InProgress && !session.ActualStartTime.HasValue)
        {
            session.ActualStartTime = DateTime.UtcNow;
        }
        else if (newStatus == SessionStatus.Completed && !session.ActualEndTime.HasValue)
        {
            session.ActualEndTime = DateTime.UtcNow;
        }

        session.UpdatedAt = DateTime.UtcNow;
        session.UpdatedBy = userId;
        
        await _sessionRepository.Update(session);

        return AppResult<LeanSession>.SuccessResult(
            session,
            $"Session status changed to {newStatus}");
    }
}
