using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class LeanSessionQueryService
{
    private readonly ILeanSessionRepository _sessionRepository;
    private readonly ILeanTopicRepository _topicRepository;
    private readonly ILeanTopicVoteRepository _voteRepository;
    private readonly ILeanParticipantRepository _participantRepository;
    private readonly IUserRepository _userRepository;

    public LeanSessionQueryService(
        ILeanSessionRepository sessionRepository,
        ILeanTopicRepository topicRepository,
        ILeanTopicVoteRepository voteRepository,
        ILeanParticipantRepository participantRepository,
        IUserRepository userRepository)
    {
        _sessionRepository = sessionRepository;
        _topicRepository = topicRepository;
        _voteRepository = voteRepository;
        _participantRepository = participantRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResults<LeanSession>> GetLeanSessionsAsync(GetLeanSessionsQuery query)
    {
        return await _sessionRepository.SearchAsync(query);
    }

    public async Task<AppResult<GetLeanSessionResult>> GetLeanSessionAsync(GetLeanSessionQuery query)
    {
        var session = await _sessionRepository.GetById(query.SessionId);
        if (session == null)
        {
            return AppResult<GetLeanSessionResult>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        // Get current topic (Discussing status)
        var currentTopic = await _topicRepository.GetCurrentTopicAsync(query.SessionId);

        // Get topic backlog (ToDiscuss status, ordered by vote count)
        var backlog = await _topicRepository.GetBySessionIdAndStatusAsync(
            query.SessionId,
            TopicStatus.ToDiscuss);
        var orderedBacklog = backlog.OrderByDescending(t => t.VoteCount)
                                   .ThenBy(t => t.DisplayOrder)
                                   .ToList();

        // Get all votes for the session
        var votes = await _voteRepository.GetBySessionIdAsync(query.SessionId);

        // Get all participants
        var participants = await _participantRepository.GetActiveParticipantsBySessionIdAsync(query.SessionId);
        var userIds = participants.Select(p => p.UserId).Distinct().ToList();
        
        var users = new System.Collections.Generic.List<User>();
        foreach (var userId in userIds)
        {
            var user = await _userRepository.GetById(userId);
            if (user != null)
            {
                users.Add(user);
            }
        }

        var result = new GetLeanSessionResult
        {
            SessionName = session.Title,
            CurrentTopic = currentTopic,
            TopicBacklog = orderedBacklog,
            TopicVotes = votes,
            Users = users
        };

        return AppResult<GetLeanSessionResult>.SuccessResult(
            result,
            "Session retrieved successfully");
    }
}
