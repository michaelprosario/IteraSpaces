using System;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class LeanTopicService : EntityService<LeanTopic>
{
    private readonly ILeanTopicRepository _topicRepository;
    private readonly ILeanTopicVoteRepository _voteRepository;
    private readonly ILeanSessionRepository _sessionRepository;

    public LeanTopicService(
        ILeanTopicRepository topicRepository,
        ILeanTopicVoteRepository voteRepository,
        ILeanSessionRepository sessionRepository) : base(topicRepository)
    {
        _topicRepository = topicRepository;
        _voteRepository = voteRepository;
        _sessionRepository = sessionRepository;
    }

    public async Task<AppResult<LeanTopic>> AddTopicAsync(AddEntityCommand<LeanTopic> command)
    {
        // Check if session exists
        var session = await _sessionRepository.GetById(command.Entity.LeanSessionId);
        if (session == null)
        {
            return AppResult<LeanTopic>.FailureResult(
                "Session not found",
                "SESSION_NOT_FOUND");
        }

        // Set initial status
        command.Entity.Status = TopicStatus.ToDiscuss;
        command.Entity.VoteCount = 0;

        return await base.AddEntityAsync(command);
    }

    public async Task<AppResult<LeanTopicVote>> VoteForLeanTopicAsync(VoteForLeanTopicCommand command)
    {
        // Check if topic exists
        var topic = await _topicRepository.GetById(command.LeanTopicId);
        if (topic == null)
        {
            return AppResult<LeanTopicVote>.FailureResult(
                "Topic not found",
                "TOPIC_NOT_FOUND");
        }

        // Check if user has already voted
        var existingVote = await _voteRepository.GetByTopicAndUserIdAsync(
            command.LeanTopicId,
            command.UserId);

        if (existingVote != null)
        {
            return AppResult<LeanTopicVote>.FailureResult(
                "User has already voted for this topic",
                "VOTE_ALREADY_EXISTS");
        }

        // Create vote
        var vote = new LeanTopicVote
        {
            Id = Guid.NewGuid().ToString(),
            LeanTopicId = command.LeanTopicId,
            UserId = command.UserId,
            LeanSessionId = command.LeanSessionId,
            VotedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = command.UserId
        };

        await _voteRepository.Add(vote);

        // Update topic vote count
        topic.VoteCount = await _voteRepository.GetVoteCountForTopicAsync(command.LeanTopicId);
        topic.UpdatedAt = DateTime.UtcNow;
        await _topicRepository.Update(topic);

        return AppResult<LeanTopicVote>.SuccessResult(
            vote,
            "Vote added successfully");
    }

    public async Task<AppResult<LeanTopic>> SetTopicStatusAsync(SetTopicStatusCommand command)
    {
        var topic = await _topicRepository.GetById(command.TopicId);
        if (topic == null)
        {
            return AppResult<LeanTopic>.FailureResult(
                "Topic not found",
                "TOPIC_NOT_FOUND");
        }

        var previousStatus = topic.Status;
        topic.Status = command.Status;
        topic.UpdatedAt = DateTime.UtcNow;
        topic.UpdatedBy = command.UserId;

        // Set discussion timestamps
        if (command.Status == TopicStatus.Discussing && previousStatus == TopicStatus.ToDiscuss)
        {
            topic.DiscussionStartedAt = DateTime.UtcNow;
        }
        else if (command.Status == TopicStatus.Discussed && previousStatus == TopicStatus.Discussing)
        {
            topic.DiscussionEndedAt = DateTime.UtcNow;
        }

        await _topicRepository.Update(topic);

        return AppResult<LeanTopic>.SuccessResult(
            topic,
            "Topic status updated successfully");
    }

    public async Task<AppResult<LeanTopic>> RemoveVoteAsync(string topicId, string userId)
    {
        // Check if vote exists
        var existingVote = await _voteRepository.GetByTopicAndUserIdAsync(topicId, userId);
        if (existingVote == null)
        {
            return AppResult<LeanTopic>.FailureResult(
                "Vote not found",
                "VOTE_NOT_FOUND");
        }

        // Remove vote
        await _voteRepository.Delete(existingVote);

        // Update topic vote count
        var topic = await _topicRepository.GetById(topicId);
        if (topic != null)
        {
            topic.VoteCount = await _voteRepository.GetVoteCountForTopicAsync(topicId);
            topic.UpdatedAt = DateTime.UtcNow;
            await _topicRepository.Update(topic);
            
            return AppResult<LeanTopic>.SuccessResult(
                topic,
                "Vote removed successfully");
        }

        return AppResult<LeanTopic>.FailureResult(
            "Topic not found after vote removal",
            "TOPIC_NOT_FOUND");
    }

    public async Task<AppResult<bool>> HasUserVotedAsync(string topicId, string userId)
    {
        var vote = await _voteRepository.GetByTopicAndUserIdAsync(topicId, userId);
        return AppResult<bool>.SuccessResult(vote != null);
    }

    public async Task<AppResult<IEnumerable<LeanTopicVote>>> GetUserVotesForSessionAsync(string sessionId, string userId)
    {
        var votes = await _voteRepository.GetBySessionAndUserIdAsync(sessionId, userId);
        return AppResult<IEnumerable<LeanTopicVote>>.SuccessResult(votes);
    }
}
