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
public class ChallengePostQueryServiceTests
{
    private IChallengePostRepository _postRepository;
    private IChallengePhaseRepository _phaseRepository;
    private IChallengeRepository _challengeRepository;
    private IChallengePostVoteRepository _voteRepository;
    private IChallengePostCommentRepository _commentRepository;
    private IUserRepository _userRepository;
    private ChallengePostQueryService _queryService;

    [SetUp]
    public void Setup()
    {
        _postRepository = Substitute.For<IChallengePostRepository>();
        _phaseRepository = Substitute.For<IChallengePhaseRepository>();
        _challengeRepository = Substitute.For<IChallengeRepository>();
        _voteRepository = Substitute.For<IChallengePostVoteRepository>();
        _commentRepository = Substitute.For<IChallengePostCommentRepository>();
        _userRepository = Substitute.For<IUserRepository>();
        _queryService = new ChallengePostQueryService(
            _postRepository,
            _phaseRepository,
            _challengeRepository,
            _voteRepository,
            _commentRepository,
            _userRepository);
    }

    #region GetChallengePostsAsync Tests

    [Test]
    public async Task GetChallengePostsAsync_SortByVotes_ShouldReturnSortedResults()
    {
        // Arrange
        var posts = new List<ChallengePost>
        {
            new ChallengePost
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Post 1",
                Description = "Test 1",
                VoteCount = 10,
                ChallengePhaseId = Guid.NewGuid().ToString(),
                SubmittedByUserId = "user123"
            },
            new ChallengePost
            {
                Id = Guid.NewGuid().ToString(),
                Title = "Post 2",
                Description = "Test 2",
                VoteCount = 5,
                ChallengePhaseId = Guid.NewGuid().ToString(),
                SubmittedByUserId = "user456"
            }
        };

        var pagedResults = new PagedResults<ChallengePost>
        {
            Data = posts,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 10,
            TotalPages = 1,
            Success = true
        };

        var query = new GetChallengePostsQuery
        {
            SortBy = "votes",
            PageNumber = 1,
            PageSize = 10
        };

        _postRepository.SearchAsync(query).Returns(pagedResults);

        // Act
        var result = await _queryService.GetChallengePostsAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
    }

    #endregion

    #region GetChallengePostAsync Tests

    [Test]
    public async Task GetChallengePostAsync_WithUserVote_ShouldIndicateVoted()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var phaseId = Guid.NewGuid().ToString();
        var challengeId = Guid.NewGuid().ToString();
        var userId = "user123";

        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = phaseId,
            Title = "Test Post",
            Description = "Test Description",
            SubmittedByUserId = "author456",
            VoteCount = 5,
            CommentCount = 3
        };

        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = challengeId,
            Name = "Test Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Open
        };

        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test",
            Status = ChallengeStatus.Open,
            CreatedByUserId = "admin123"
        };

        var user = new User
        {
            Id = "author456",
            DisplayName = "Test Author",
            Email = "author@test.com"
        };

        var comments = new List<ChallengePostComment>
        {
            new ChallengePostComment
            {
                Id = Guid.NewGuid().ToString(),
                ChallengePostId = postId,
                UserId = userId,
                Content = "Great post!",
                Status = CommentStatus.Active,
                IsDeleted = false
            }
        };

        var query = new GetChallengePostQuery
        {
            ChallengePostId = postId,
            RequestingUserId = userId
        };

        _postRepository.GetById(postId).Returns(post);
        _phaseRepository.GetById(phaseId).Returns(phase);
        _challengeRepository.GetById(challengeId).Returns(challenge);
        _userRepository.GetById("author456").Returns(user);
        _voteRepository.HasUserVotedAsync(postId, userId).Returns(true);
        _commentRepository.GetByPostIdAsync(postId).Returns(comments);

        // Act
        var result = await _queryService.GetChallengePostAsync(query);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Post.Id, Is.EqualTo(postId));
        Assert.That(result.Data.Phase.Id, Is.EqualTo(phaseId));
        Assert.That(result.Data.Challenge.Id, Is.EqualTo(challengeId));
        Assert.That(result.Data.SubmittedByUsername, Is.EqualTo("Test Author"));
        Assert.That(result.Data.HasUserVoted, Is.True);
        Assert.That(result.Data.Comments.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetChallengePostAsync_WithoutUserVote_ShouldIndicateNotVoted()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var phaseId = Guid.NewGuid().ToString();
        var challengeId = Guid.NewGuid().ToString();
        var userId = "user123";

        var post = new ChallengePost
        {
            Id = postId,
            ChallengePhaseId = phaseId,
            Title = "Test Post",
            Description = "Test Description",
            SubmittedByUserId = "author456",
            VoteCount = 5,
            CommentCount = 0
        };

        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = challengeId,
            Name = "Test Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Open
        };

        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test",
            Status = ChallengeStatus.Open,
            CreatedByUserId = "admin123"
        };

        var user = new User
        {
            Id = "author456",
            DisplayName = "Test Author",
            Email = "author@test.com"
        };

        var query = new GetChallengePostQuery
        {
            ChallengePostId = postId,
            RequestingUserId = userId
        };

        _postRepository.GetById(postId).Returns(post);
        _phaseRepository.GetById(phaseId).Returns(phase);
        _challengeRepository.GetById(challengeId).Returns(challenge);
        _userRepository.GetById("author456").Returns(user);
        _voteRepository.HasUserVotedAsync(postId, userId).Returns(false);
        _commentRepository.GetByPostIdAsync(postId).Returns(new List<ChallengePostComment>());

        // Act
        var result = await _queryService.GetChallengePostAsync(query);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.HasUserVoted, Is.False);
    }

    [Test]
    public async Task GetChallengePostAsync_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetChallengePostQuery
        {
            ChallengePostId = ""
        };

        // Act
        var result = await _queryService.GetChallengePostAsync(query);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_POST_ID"));
    }

    [Test]
    public async Task GetChallengePostAsync_WithNonExistentPost_ShouldReturnFailure()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var query = new GetChallengePostQuery
        {
            ChallengePostId = postId
        };

        _postRepository.GetById(postId).Returns((ChallengePost?)null);

        // Act
        var result = await _queryService.GetChallengePostAsync(query);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("POST_NOT_FOUND"));
    }

    #endregion

    #region GetCommentsAsync Tests

    [Test]
    public async Task GetCommentsAsync_WithValidPostId_ShouldReturnPagedComments()
    {
        // Arrange
        var postId = Guid.NewGuid().ToString();
        var comments = new List<ChallengePostComment>
        {
            new ChallengePostComment
            {
                Id = Guid.NewGuid().ToString(),
                ChallengePostId = postId,
                UserId = "user123",
                Content = "Comment 1",
                Status = CommentStatus.Active
            },
            new ChallengePostComment
            {
                Id = Guid.NewGuid().ToString(),
                ChallengePostId = postId,
                UserId = "user456",
                Content = "Comment 2",
                Status = CommentStatus.Active
            }
        };

        var pagedResults = new PagedResults<ChallengePostComment>
        {
            Data = comments,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 20,
            TotalPages = 1,
            Success = true
        };

        var query = new GetChallengePostCommentsQuery
        {
            ChallengePostId = postId,
            PageNumber = 1,
            PageSize = 20
        };

        _commentRepository.GetPagedByPostIdAsync(query).Returns(pagedResults);

        // Act
        var result = await _queryService.GetCommentsAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
    }

    #endregion
}
