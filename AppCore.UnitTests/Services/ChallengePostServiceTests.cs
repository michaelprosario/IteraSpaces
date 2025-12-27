using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.DTOs;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using NSubstitute;
using NUnit.Framework;

namespace AppCore.UnitTests.Services;

[TestFixture]
public class ChallengePostServiceTests
{
    private IChallengePostRepository _postRepository;
    private IChallengePhaseRepository _phaseRepository;
    private IChallengePostVoteRepository _voteRepository;
    private IChallengePostCommentRepository _commentRepository;
    private ChallengePostService _postService;

    [SetUp]
    public void Setup()
    {
        _postRepository = Substitute.For<IChallengePostRepository>();
        _phaseRepository = Substitute.For<IChallengePhaseRepository>();
        _voteRepository = Substitute.For<IChallengePostVoteRepository>();
        _commentRepository = Substitute.For<IChallengePostCommentRepository>();
        _postService = new ChallengePostService(_postRepository, _phaseRepository, _voteRepository, _commentRepository);
    }

    #region StoreEntityAsync Tests

    [Test]
    public async Task StoreEntityAsync_WithOpenPhase_ShouldCreatePost()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = Guid.NewGuid().ToString(),
            Name = "Open Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Open
        };

        var post = new ChallengePost
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePhaseId = phaseId,
            Title = "Great Idea",
            Description = "This is a great idea",
            SubmittedByUserId = "user123"
        };

        var command = new StoreEntityCommand<ChallengePost>(post)
        {
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns(phase);
        _postRepository.RecordExists(post.Id).Returns(false);
        _postRepository.Add(Arg.Any<ChallengePost>()).Returns(callInfo => callInfo.Arg<ChallengePost>());

        // Act
        var result = await _postService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Title, Is.EqualTo(post.Title));
        await _postRepository.Received(1).Add(Arg.Any<ChallengePost>());
    }

    [Test]
    public async Task StoreEntityAsync_WithClosedPhase_ShouldReturnFailure()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = Guid.NewGuid().ToString(),
            Name = "Closed Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Closed
        };

        var post = new ChallengePost
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePhaseId = phaseId,
            Title = "Great Idea",
            Description = "This is a great idea",
            SubmittedByUserId = "user123"
        };

        var command = new StoreEntityCommand<ChallengePost>(post)
        {
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns(phase);
        _postRepository.RecordExists(post.Id).Returns(false);

        // Act
        var result = await _postService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("PHASE_NOT_OPEN"));
    }

    [Test]
    public async Task StoreEntityAsync_UpdateExistingPost_ShouldSucceed()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = Guid.NewGuid().ToString(),
            Name = "Open Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Open
        };

        var existingPost = new ChallengePost
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePhaseId = phaseId,
            Title = "Original Title",
            Description = "Original Description",
            SubmittedByUserId = "user123",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "user123"
        };

        var updatedPost = new ChallengePost
        {
            Id = existingPost.Id,
            ChallengePhaseId = phaseId,
            Title = "Updated Title",
            Description = "Updated Description",
            SubmittedByUserId = "user123"
        };

        var command = new StoreEntityCommand<ChallengePost>(updatedPost)
        {
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns(phase);
        _postRepository.RecordExists(existingPost.Id).Returns(true);
        _postRepository.GetById(existingPost.Id).Returns(existingPost);

        // Act
        var result = await _postService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Title, Is.EqualTo("Updated Title"));
        await _postRepository.Received(1).Update(Arg.Any<ChallengePost>());
    }

    #endregion

    #region VoteAsync Tests

    [Test]
    public async Task VoteAsync_WithValidCommand_ShouldAddVoteAndIncrementCount()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            VoteCount = 0
        };

        var command = new VoteChallengePostCommand
        {
            ChallengePostId = postId,
            UserId = "voter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _voteRepository.GetVoteAsync(postId, "voter456").Returns((ChallengePostVote?)null);
        _voteRepository.Add(Arg.Any<ChallengePostVote>()).Returns(callInfo => callInfo.Arg<ChallengePostVote>());

        // Act
        var result = await _postService.VoteAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.VoteCount, Is.EqualTo(1));
        await _voteRepository.Received(1).Add(Arg.Any<ChallengePostVote>());
        await _postRepository.Received(1).Update(Arg.Is<ChallengePost>(p => p.VoteCount == 1));
    }

    [Test]
    public async Task VoteAsync_WhenAlreadyVoted_ShouldReturnFailure()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            VoteCount = 1
        };

        var existingVote = new ChallengePostVote
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePostId = postId,
            UserId = "voter456",
            VotedAt = DateTime.UtcNow
        };

        var command = new VoteChallengePostCommand
        {
            ChallengePostId = postId,
            UserId = "voter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _voteRepository.GetVoteAsync(postId, "voter456").Returns(existingVote);

        // Act
        var result = await _postService.VoteAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("ALREADY_VOTED"));
        await _voteRepository.DidNotReceive().Add(Arg.Any<ChallengePostVote>());
    }

    [Test]
    public async Task VoteAsync_OnOwnPost_ShouldReturnFailure()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var userId = "user123";
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = userId,
            VoteCount = 0
        };

        var command = new VoteChallengePostCommand
        {
            ChallengePostId = postId,
            UserId = userId
        };

        _postRepository.GetById(postId).Returns(post);

        // Act
        var result = await _postService.VoteAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("CANNOT_VOTE_OWN_POST"));
        await _voteRepository.DidNotReceive().Add(Arg.Any<ChallengePostVote>());
    }

    #endregion

    #region RemoveVoteAsync Tests

    [Test]
    public async Task RemoveVoteAsync_WhenVoteExists_ShouldRemoveVoteAndDecrementCount()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            VoteCount = 3
        };

        var existingVote = new ChallengePostVote
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePostId = postId,
            UserId = "voter456",
            VotedAt = DateTime.UtcNow
        };

        var command = new RemoveVoteChallengePostCommand
        {
            ChallengePostId = postId,
            UserId = "voter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _voteRepository.GetVoteAsync(postId, "voter456").Returns(existingVote);

        // Act
        var result = await _postService.RemoveVoteAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        await _voteRepository.Received(1).Delete(existingVote);
        await _postRepository.Received(1).Update(Arg.Is<ChallengePost>(p => p.VoteCount == 2));
    }

    [Test]
    public async Task RemoveVoteAsync_WhenNoVote_ShouldReturnFailure()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            VoteCount = 0
        };

        var command = new RemoveVoteChallengePostCommand
        {
            ChallengePostId = postId,
            UserId = "voter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _voteRepository.GetVoteAsync(postId, "voter456").Returns((ChallengePostVote?)null);

        // Act
        var result = await _postService.RemoveVoteAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("VOTE_NOT_FOUND"));
        await _voteRepository.DidNotReceive().Delete(Arg.Any<ChallengePostVote>());
    }

    #endregion

    #region StoreCommentAsync Tests

    [Test]
    public async Task StoreCommentAsync_WithValidCommand_ShouldAddCommentAndIncrementCount()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            CommentCount = 0
        };

        var comment = new ChallengePostComment
        {
            Id = "",
            ChallengePostId = postId,
            UserId = "commenter456",
            Content = "Great idea!",
            Status = CommentStatus.Active
        };

        var command = new StoreEntityCommand<ChallengePostComment>(comment)
        {
            UserId = "commenter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _commentRepository.RecordExists(Arg.Any<string>()).Returns(false);
        _commentRepository.Add(Arg.Any<ChallengePostComment>()).Returns(callInfo => callInfo.Arg<ChallengePostComment>());

        // Act
        var result = await _postService.StoreCommentAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Content, Is.EqualTo("Great idea!"));
        await _commentRepository.Received(1).Add(Arg.Any<ChallengePostComment>());
        await _postRepository.Received(1).Update(Arg.Is<ChallengePost>(p => p.CommentCount == 1));
    }

    [Test]
    public async Task StoreCommentAsync_UpdateOwnComment_ShouldSucceed()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            CommentCount = 1
        };

        var existingComment = new ChallengePostComment
        {
            Id = Guid.NewGuid().ToString(),
            ChallengePostId = postId,
            UserId = "commenter456",
            Content = "Original comment",
            Status = CommentStatus.Active
        };

        var updatedComment = new ChallengePostComment
        {
            Id = existingComment.Id,
            ChallengePostId = postId,
            UserId = "commenter456",
            Content = "Updated comment",
            Status = CommentStatus.Active
        };

        var command = new StoreEntityCommand<ChallengePostComment>(updatedComment)
        {
            UserId = "commenter456"
        };

        _postRepository.GetById(postId).Returns(post);
        _commentRepository.RecordExists(existingComment.Id).Returns(true);

        // Act
        var result = await _postService.StoreCommentAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Content, Is.EqualTo("Updated comment"));
        Assert.That(result.Data.Status, Is.EqualTo(CommentStatus.Edited));
        await _commentRepository.Received(1).Update(Arg.Any<ChallengePostComment>());
    }

    #endregion

    #region DeleteCommentAsync Tests

    [Test]
    public async Task DeleteCommentAsync_WithOwnComment_ShouldDecrementCount()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var commentId = Guid.NewGuid().ToString();
        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = Guid.NewGuid().ToString(),
            Title = "Test Post",
            Description = "Test",
            SubmittedByUserId = "author123",
            CommentCount = 2
        };

        var comment = new ChallengePostComment
        {
            Id = commentId,
            ChallengePostId = postId,
            UserId = "commenter456",
            Content = "Test comment",
            Status = CommentStatus.Active
        };

        var command = new DeleteEntityCommand(commentId)
        {
            UserId = "commenter456"
        };

        _commentRepository.GetById(commentId).Returns(comment);
        _postRepository.GetById(postId).Returns(post);

        // Act
        var result = await _postService.DeleteCommentAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        await _commentRepository.Received(1).Update(Arg.Is<ChallengePostComment>(c => 
            c.IsDeleted && c.Status == CommentStatus.Deleted));
        await _postRepository.Received(1).Update(Arg.Is<ChallengePost>(p => p.CommentCount == 1));
    }

    #endregion
}
