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
public class ChallengeQueryServiceTests
{
    private IChallengeRepository _challengeRepository;
    private IChallengePhaseRepository _phaseRepository;
    private IChallengePostRepository _postRepository;
    private ChallengeQueryService _queryService;

    [SetUp]
    public void Setup()
    {
        _challengeRepository = Substitute.For<IChallengeRepository>();
        _phaseRepository = Substitute.For<IChallengePhaseRepository>();
        _postRepository = Substitute.For<IChallengePostRepository>();
        _queryService = new ChallengeQueryService(_challengeRepository, _phaseRepository, _postRepository);
    }

    #region GetChallengesAsync Tests

    [Test]
    public async Task GetChallengesAsync_WithFilters_ShouldReturnFilteredResults()
    {
        // Arrange
        var challenges = new List<Challenge>
        {
            new Challenge
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Challenge 1",
                Description = "Test 1",
                Status = ChallengeStatus.Open,
                CreatedByUserId = "user123"
            },
            new Challenge
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Challenge 2",
                Description = "Test 2",
                Status = ChallengeStatus.Open,
                CreatedByUserId = "user123"
            }
        };

        var pagedResults = new PagedResults<Challenge>
        {
            Data = challenges,
            TotalCount = 2,
            CurrentPage = 1,
            PageSize = 10,
            TotalPages = 1,
            Success = true
        };

        var query = new GetChallengesQuery
        {
            Status = ChallengeStatus.Open,
            PageNumber = 1,
            PageSize = 10
        };

        _challengeRepository.SearchAsync(query).Returns(pagedResults);

        // Act
        var result = await _queryService.GetChallengesAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Count, Is.EqualTo(2));
        Assert.That(result.TotalCount, Is.EqualTo(2));
    }

    [Test]
    public async Task GetChallengesAsync_WithPagination_ShouldReturnPagedResults()
    {
        // Arrange
        var challenges = new List<Challenge>
        {
            new Challenge
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Challenge 1",
                Description = "Test 1",
                Status = ChallengeStatus.Open,
                CreatedByUserId = "user123"
            }
        };

        var pagedResults = new PagedResults<Challenge>
        {
            Data = challenges,
            TotalCount = 20,
            CurrentPage = 2,
            PageSize = 10,
            TotalPages = 2,
            Success = true
        };

        var query = new GetChallengesQuery
        {
            PageNumber = 2,
            PageSize = 10
        };

        _challengeRepository.SearchAsync(query).Returns(pagedResults);

        // Act
        var result = await _queryService.GetChallengesAsync(query);

        // Assert
        Assert.That(result.CurrentPage, Is.EqualTo(2));
        Assert.That(result.PageSize, Is.EqualTo(10));
        Assert.That(result.TotalPages, Is.EqualTo(2));
    }

    #endregion

    #region GetChallengeAsync Tests

    [Test]
    public async Task GetChallengeAsync_WithValidId_ShouldReturnChallengeWithPhases()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test Description",
            Status = ChallengeStatus.Open,
            CreatedByUserId = "user123"
        };

        var phases = new List<ChallengePhase>
        {
            new ChallengePhase
            {
                Id = Guid.NewGuid().ToString(),
                ChallengeId = challengeId,
                Name = "Phase 1",
                Description = "Test Phase 1",
                Status = ChallengePhaseStatus.Open
            },
            new ChallengePhase
            {
                Id = Guid.NewGuid().ToString(),
                ChallengeId = challengeId,
                Name = "Phase 2",
                Description = "Test Phase 2",
                Status = ChallengePhaseStatus.Planned
            }
        };

        var query = new GetChallengeQuery
        {
            ChallengeId = challengeId
        };

        _challengeRepository.GetById(challengeId).Returns(challenge);
        _phaseRepository.GetByChallengeIdAsync(challengeId).Returns(phases);
        _postRepository.GetPostCountByChallengeIdAsync(challengeId).Returns(15);

        // Act
        var result = await _queryService.GetChallengeAsync(query);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Challenge.Id, Is.EqualTo(challengeId));
        Assert.That(result.Data.Phases.Count, Is.EqualTo(2));
        Assert.That(result.Data.TotalPosts, Is.EqualTo(15));
    }

    [Test]
    public async Task GetChallengeAsync_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        var query = new GetChallengeQuery
        {
            ChallengeId = ""
        };

        // Act
        var result = await _queryService.GetChallengeAsync(query);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_CHALLENGE_ID"));
    }

    [Test]
    public async Task GetChallengeAsync_WithNonExistentChallenge_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var query = new GetChallengeQuery
        {
            ChallengeId = challengeId
        };

        _challengeRepository.GetById(challengeId).Returns((Challenge?)null);

        // Act
        var result = await _queryService.GetChallengeAsync(query);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("CHALLENGE_NOT_FOUND"));
    }

    #endregion
}
