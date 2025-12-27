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
public class ChallengeServiceTests
{
    private IChallengeRepository _challengeRepository;
    private IChallengePhaseRepository _phaseRepository;
    private ChallengeService _challengeService;

    [SetUp]
    public void Setup()
    {
        _challengeRepository = Substitute.For<IChallengeRepository>();
        _phaseRepository = Substitute.For<IChallengePhaseRepository>();
        _challengeService = new ChallengeService(_challengeRepository, _phaseRepository);
    }

    #region StoreEntityAsync Tests

    [Test]
    public async Task StoreEntityAsync_WithNewChallenge_ShouldCreateChallenge()
    {
        // Arrange
        var challenge = new Challenge
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Community Garden Challenge",
            Description = "Create a community garden",
            Status = ChallengeStatus.Draft,
            CreatedByUserId = "user123"
        };

        var command = new StoreEntityCommand<Challenge>(challenge)
        {
            UserId = "user123"
        };

        _challengeRepository.RecordExists(challenge.Id).Returns(false);
        _challengeRepository.Add(Arg.Any<Challenge>()).Returns(callInfo => callInfo.Arg<Challenge>());

        // Act
        var result = await _challengeService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Name, Is.EqualTo(challenge.Name));
        await _challengeRepository.Received(1).Add(Arg.Any<Challenge>());
    }

    [Test]
    public async Task StoreEntityAsync_WithExistingId_ShouldUpdateChallenge()
    {
        // Arrange
        var existingChallenge = new Challenge
        {
            Id = Guid.NewGuid().ToString(),
            Name = "Original Name",
            Description = "Original Description",
            Status = ChallengeStatus.Draft,
            CreatedByUserId = "user123",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedBy = "user123"
        };

        var updatedChallenge = new Challenge
        {
            Id = existingChallenge.Id,
            Name = "Updated Name",
            Description = "Updated Description",
            Status = ChallengeStatus.Open,
            CreatedByUserId = "user123"
        };

        var command = new StoreEntityCommand<Challenge>(updatedChallenge)
        {
            UserId = "user123"
        };

        _challengeRepository.RecordExists(existingChallenge.Id).Returns(true);
        _challengeRepository.GetById(existingChallenge.Id).Returns(existingChallenge);

        // Act
        var result = await _challengeService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Name, Is.EqualTo("Updated Name"));
        await _challengeRepository.Received(1).Update(Arg.Any<Challenge>());
    }

    #endregion

    #region DeleteEntityAsync Tests

    [Test]
    public async Task DeleteEntityAsync_WithActivePhases_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test Description",
            CreatedByUserId = "user123"
        };

        var activePhases = new List<ChallengePhase>
        {
            new ChallengePhase
            {
                Id = Guid.NewGuid().ToString(),
                ChallengeId = challengeId,
                Name = "Phase 1",
                Description = "Active phase",
                Status = ChallengePhaseStatus.Open,
                IsDeleted = false
            }
        };

        var command = new DeleteEntityCommand(challengeId)
        {
            UserId = "user123"
        };

        _challengeRepository.GetById(challengeId).Returns(challenge);
        _phaseRepository.GetByChallengeIdAsync(challengeId).Returns(activePhases);

        // Act
        var result = await _challengeService.DeleteEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("HAS_ACTIVE_PHASES"));
        await _challengeRepository.DidNotReceive().Update(Arg.Any<Challenge>());
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Test]
    public async Task UpdateStatusAsync_WithValidCommand_ShouldUpdateStatus()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test Description",
            Status = ChallengeStatus.Draft,
            CreatedByUserId = "user123"
        };

        var command = new UpdateChallengeStatusCommand
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            UserId = "user123"
        };

        _challengeRepository.GetById(challengeId).Returns(challenge);

        // Act
        var result = await _challengeService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Status, Is.EqualTo(ChallengeStatus.Open));
        await _challengeRepository.Received(1).Update(Arg.Is<Challenge>(c => c.Status == ChallengeStatus.Open));
    }

    [Test]
    public async Task UpdateStatusAsync_FromArchivedToOther_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var challenge = new Challenge
        {
            Id = challengeId,
            Name = "Test Challenge",
            Description = "Test Description",
            Status = ChallengeStatus.Archived,
            CreatedByUserId = "user123"
        };

        var command = new UpdateChallengeStatusCommand
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            UserId = "user123"
        };

        _challengeRepository.GetById(challengeId).Returns(challenge);

        // Act
        var result = await _challengeService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_STATUS_TRANSITION"));
    }

    [Test]
    public async Task UpdateStatusAsync_WithInvalidChallengeId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateChallengeStatusCommand
        {
            ChallengeId = "",
            Status = ChallengeStatus.Open,
            UserId = "user123"
        };

        // Act
        var result = await _challengeService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_CHALLENGE_ID"));
    }

    [Test]
    public async Task UpdateStatusAsync_WithNonExistentChallenge_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var command = new UpdateChallengeStatusCommand
        {
            ChallengeId = challengeId,
            Status = ChallengeStatus.Open,
            UserId = "user123"
        };

        _challengeRepository.GetById(challengeId).Returns((Challenge?)null);

        // Act
        var result = await _challengeService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("CHALLENGE_NOT_FOUND"));
    }

    #endregion
}
