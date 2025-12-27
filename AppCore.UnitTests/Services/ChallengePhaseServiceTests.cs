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
public class ChallengePhaseServiceTests
{
    private IChallengePhaseRepository _phaseRepository;
    private IChallengeRepository _challengeRepository;
    private IChallengePostRepository _postRepository;
    private ChallengePhaseService _phaseService;

    [SetUp]
    public void Setup()
    {
        _phaseRepository = Substitute.For<IChallengePhaseRepository>();
        _challengeRepository = Substitute.For<IChallengeRepository>();
        _postRepository = Substitute.For<IChallengePostRepository>();
        _phaseService = new ChallengePhaseService(_phaseRepository, _challengeRepository, _postRepository);
    }

    #region StoreEntityAsync Tests

    [Test]
    public async Task StoreEntityAsync_WithNewPhase_ShouldCreatePhase()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = Guid.NewGuid().ToString(),
            ChallengeId = challengeId,
            Name = "Ideation Phase",
            Description = "Share your ideas",
            Status = ChallengePhaseStatus.Planned,
            StartDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        var command = new StoreEntityCommand<ChallengePhase>(phase)
        {
            UserId = "user123"
        };

        _challengeRepository.RecordExists(challengeId).Returns(true);
        _phaseRepository.RecordExists(phase.Id).Returns(false);
        _phaseRepository.GetByChallengeIdAsync(challengeId).Returns(new List<ChallengePhase>());
        _phaseRepository.Add(Arg.Any<ChallengePhase>()).Returns(callInfo => callInfo.Arg<ChallengePhase>());

        // Act
        var result = await _phaseService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data.Name, Is.EqualTo(phase.Name));
        await _phaseRepository.Received(1).Add(Arg.Any<ChallengePhase>());
    }

    [Test]
    public async Task StoreEntityAsync_WithInvalidChallengeId_ShouldReturnFailure()
    {
        // Arrange
        var phase = new ChallengePhase
        {
            Id = Guid.NewGuid().ToString(),
            ChallengeId = "invalid-challenge-id",
            Name = "Test Phase",
            Description = "Test Description",
            Status = ChallengePhaseStatus.Planned
        };

        var command = new StoreEntityCommand<ChallengePhase>(phase)
        {
            UserId = "user123"
        };

        _challengeRepository.RecordExists("invalid-challenge-id").Returns(false);

        // Act
        var result = await _phaseService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("CHALLENGE_NOT_FOUND"));
    }

    [Test]
    public async Task StoreEntityAsync_WithOverlappingDates_ShouldReturnFailure()
    {
        // Arrange
        var challengeId = Guid.NewGuid().ToString();
        var startDate = DateTime.UtcNow.AddDays(10);
        var endDate = DateTime.UtcNow.AddDays(20);

        var existingPhase = new ChallengePhase
        {
            Id = Guid.NewGuid().ToString(),
            ChallengeId = challengeId,
            Name = "Existing Phase",
            Description = "Existing",
            Status = ChallengePhaseStatus.Open,
            StartDate = DateTime.UtcNow.AddDays(5),
            EndDate = DateTime.UtcNow.AddDays(15),
            IsDeleted = false
        };

        var newPhase = new ChallengePhase
        {
            Id = Guid.NewGuid().ToString(),
            ChallengeId = challengeId,
            Name = "Overlapping Phase",
            Description = "Test",
            Status = ChallengePhaseStatus.Planned,
            StartDate = startDate,
            EndDate = endDate
        };

        var command = new StoreEntityCommand<ChallengePhase>(newPhase)
        {
            UserId = "user123"
        };

        _challengeRepository.RecordExists(challengeId).Returns(true);
        _phaseRepository.RecordExists(newPhase.Id).Returns(false);
        _phaseRepository.GetByChallengeIdAsync(challengeId).Returns(new List<ChallengePhase> { existingPhase });

        // Act
        var result = await _phaseService.StoreEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("OVERLAPPING_DATES"));
    }

    #endregion

    #region DeleteEntityAsync Tests

    [Test]
    public async Task DeleteEntityAsync_WithExistingPosts_ShouldReturnFailure()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = Guid.NewGuid().ToString(),
            Name = "Test Phase",
            Description = "Test Description",
            Status = ChallengePhaseStatus.Open
        };

        var command = new DeleteEntityCommand(phaseId)
        {
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns(phase);
        _postRepository.GetPostCountByPhaseIdAsync(phaseId).Returns(5);

        // Act
        var result = await _phaseService.DeleteEntityAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("HAS_POSTS"));
        await _phaseRepository.DidNotReceive().Update(Arg.Any<ChallengePhase>());
    }

    #endregion

    #region UpdateStatusAsync Tests

    [Test]
    public async Task UpdateStatusAsync_WithValidCommand_ShouldUpdateStatus()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var phase = new ChallengePhase
        {
            Id = phaseId,
            ChallengeId = Guid.NewGuid().ToString(),
            Name = "Test Phase",
            Description = "Test Description",
            Status = ChallengePhaseStatus.Planned
        };

        var command = new UpdateChallengePhaseStatusCommand
        {
            ChallengePhaseId = phaseId,
            Status = ChallengePhaseStatus.Open,
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns(phase);

        // Act
        var result = await _phaseService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data.Status, Is.EqualTo(ChallengePhaseStatus.Open));
        await _phaseRepository.Received(1).Update(Arg.Is<ChallengePhase>(p => p.Status == ChallengePhaseStatus.Open));
    }

    [Test]
    public async Task UpdateStatusAsync_WithInvalidPhaseId_ShouldReturnFailure()
    {
        // Arrange
        var command = new UpdateChallengePhaseStatusCommand
        {
            ChallengePhaseId = "",
            Status = ChallengePhaseStatus.Open,
            UserId = "user123"
        };

        // Act
        var result = await _phaseService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("INVALID_PHASE_ID"));
    }

    [Test]
    public async Task UpdateStatusAsync_WithNonExistentPhase_ShouldReturnFailure()
    {
        // Arrange
        var phaseId = Guid.NewGuid().ToString();
        var command = new UpdateChallengePhaseStatusCommand
        {
            ChallengePhaseId = phaseId,
            Status = ChallengePhaseStatus.Open,
            UserId = "user123"
        };

        _phaseRepository.GetById(phaseId).Returns((ChallengePhase?)null);

        // Act
        var result = await _phaseService.UpdateStatusAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorCode, Is.EqualTo("PHASE_NOT_FOUND"));
    }

    #endregion
}
