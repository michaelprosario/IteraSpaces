using System;
using System.Linq;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

public class ChallengePostService : EntityService<ChallengePost>
{
    private readonly IChallengePostRepository _postRepository;
    private readonly IChallengePhaseRepository _phaseRepository;
    private readonly IChallengePostVoteRepository _voteRepository;
    private readonly IChallengePostCommentRepository _commentRepository;

    public ChallengePostService(
        IChallengePostRepository postRepository,
        IChallengePhaseRepository phaseRepository,
        IChallengePostVoteRepository voteRepository,
        IChallengePostCommentRepository commentRepository)
        : base(postRepository)
    {
        _postRepository = postRepository;
        _phaseRepository = phaseRepository;
        _voteRepository = voteRepository;
        _commentRepository = commentRepository;
    }

    /// <summary>
    /// Custom StoreEntityAsync to validate phase is open for new posts
    /// </summary>
    public new async Task<AppResult<ChallengePost>> StoreEntityAsync(StoreEntityCommand<ChallengePost> command)
    {
        var phase = await _phaseRepository.GetById(command.Entity.ChallengePhaseId);
        if (phase == null)
        {
            return AppResult<ChallengePost>.FailureResult(
                "Challenge phase not found",
                "PHASE_NOT_FOUND");
        }

        // Check if this is a new post (creating) or updating existing
        var isNewPost = string.IsNullOrEmpty(command.Entity.Id) || 
                        !await _postRepository.RecordExists(command.Entity.Id);

        // Only validate phase status for new posts
        if (isNewPost && phase.Status != ChallengePhaseStatus.Open)
        {
            return AppResult<ChallengePost>.FailureResult(
                "Can only submit posts to open challenge phases",
                "PHASE_NOT_OPEN");
        }

        return await base.StoreEntityAsync(command);
    }

    /// <summary>
    /// Vote for a challenge post
    /// </summary>
    public async Task<AppResult<ChallengePost>> VoteAsync(VoteChallengePostCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ChallengePostId))
        {
            return AppResult<ChallengePost>.FailureResult(
                "ChallengePostId is required",
                "INVALID_POST_ID");
        }

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return AppResult<ChallengePost>.FailureResult(
                "UserId is required",
                "INVALID_USER_ID");
        }

        var post = await _postRepository.GetById(command.ChallengePostId);
        if (post == null)
        {
            return AppResult<ChallengePost>.FailureResult(
                "Post not found",
                "POST_NOT_FOUND");
        }

        // Users cannot vote for their own posts
        if (post.SubmittedByUserId == command.UserId)
        {
            return AppResult<ChallengePost>.FailureResult(
                "Cannot vote for your own post",
                "CANNOT_VOTE_OWN_POST");
        }

        // Check if user has already voted
        var existingVote = await _voteRepository.GetVoteAsync(command.ChallengePostId, command.UserId);
        if (existingVote != null)
        {
            return AppResult<ChallengePost>.FailureResult(
                "You have already voted for this post",
                "ALREADY_VOTED");
        }

        // Create vote
        var vote = new ChallengePostVote
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePostId = command.ChallengePostId,
            UserId = command.UserId,
            VotedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = command.UserId
        };

        await _voteRepository.Add(vote);

        // Increment vote count on post
        post.VoteCount++;
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedBy = command.UserId;

        await _postRepository.Update(post);

        return AppResult<ChallengePost>.SuccessResult(
            post,
            "Vote added successfully");
    }

    /// <summary>
    /// Remove vote from a challenge post
    /// </summary>
    public async Task<AppResult<bool>> RemoveVoteAsync(RemoveVoteChallengePostCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ChallengePostId))
        {
            return AppResult<bool>.FailureResult(
                "ChallengePostId is required",
                "INVALID_POST_ID");
        }

        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return AppResult<bool>.FailureResult(
                "UserId is required",
                "INVALID_USER_ID");
        }

        var post = await _postRepository.GetById(command.ChallengePostId);
        if (post == null)
        {
            return AppResult<bool>.FailureResult(
                "Post not found",
                "POST_NOT_FOUND");
        }

        // Check if vote exists
        var existingVote = await _voteRepository.GetVoteAsync(command.ChallengePostId, command.UserId);
        if (existingVote == null)
        {
            return AppResult<bool>.FailureResult(
                "Vote not found",
                "VOTE_NOT_FOUND");
        }

        // Delete vote (hard delete since it's just a relationship)
        await _voteRepository.Delete(existingVote);

        // Decrement vote count on post
        post.VoteCount = Math.Max(0, post.VoteCount - 1);
        post.UpdatedAt = DateTime.UtcNow;
        post.UpdatedBy = command.UserId;

        await _postRepository.Update(post);

        return AppResult<bool>.SuccessResult(
            true,
            "Vote removed successfully");
    }

    /// <summary>
    /// Store (add or update) a comment on a challenge post
    /// </summary>
    public async Task<AppResult<ChallengePostComment>> StoreCommentAsync(StoreEntityCommand<ChallengePostComment> command)
    {
        // Validate post exists
        var post = await _postRepository.GetById(command.Entity.ChallengePostId);
        if (post == null)
        {
            return AppResult<ChallengePostComment>.FailureResult(
                "Post not found",
                "POST_NOT_FOUND");
        }

        // Validate comment entity
        var validator = new ChallengePostCommentValidator();
        var validationResult = await validator.ValidateAsync(command.Entity);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<ChallengePostComment>.ValidationFailure(errors);
        }

        var isNewComment = string.IsNullOrEmpty(command.Entity.Id) || 
                           !await _commentRepository.RecordExists(command.Entity.Id);

        ChallengePostComment savedComment;

        if (isNewComment)
        {
            // Create new comment
            command.Entity.Id = Guid.NewGuid().ToString();
            command.Entity.CreatedAt = DateTime.UtcNow;
            command.Entity.CreatedBy = command.UserId;

            savedComment = await _commentRepository.Add(command.Entity);

            // Increment comment count on post
            post.CommentCount++;
        }
        else
        {
            // Update existing comment
            command.Entity.UpdatedAt = DateTime.UtcNow;
            command.Entity.UpdatedBy = command.UserId;
            command.Entity.Status = CommentStatus.Edited;

            await _commentRepository.Update(command.Entity);
            savedComment = command.Entity;
        }

        // Update post's UpdatedAt timestamp
        post.UpdatedAt = DateTime.UtcNow;
        await _postRepository.Update(post);

        return AppResult<ChallengePostComment>.SuccessResult(
            savedComment,
            isNewComment ? "Comment added successfully" : "Comment updated successfully");
    }

    /// <summary>
    /// Delete a comment from a challenge post
    /// </summary>
    public async Task<AppResult<bool>> DeleteCommentAsync(DeleteEntityCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.EntityId))
        {
            return AppResult<bool>.FailureResult(
                "CommentId is required",
                "INVALID_COMMENT_ID");
        }

        var comment = await _commentRepository.GetById(command.EntityId);
        if (comment == null)
        {
            return AppResult<bool>.FailureResult(
                "Comment not found",
                "COMMENT_NOT_FOUND");
        }

        // Soft delete comment
        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        comment.DeletedBy = command.UserId;
        comment.Status = CommentStatus.Deleted;

        await _commentRepository.Update(comment);

        // Decrement comment count on post
        var post = await _postRepository.GetById(comment.ChallengePostId);
        if (post != null)
        {
            post.CommentCount = Math.Max(0, post.CommentCount - 1);
            post.UpdatedAt = DateTime.UtcNow;
            await _postRepository.Update(post);
        }

        return AppResult<bool>.SuccessResult(
            true,
            "Comment deleted successfully");
    }
}
