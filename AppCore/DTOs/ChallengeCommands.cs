using AppCore.Entities;
using AppCore.Services;

namespace AppCore.DTOs;

// Custom command for challenge status updates
public class UpdateChallengeStatusCommand : BaseRequest
{
    public string ChallengeId { get; set; } = string.Empty;
    public ChallengeStatus Status { get; set; }
}

// Custom command for phase status updates
public class UpdateChallengePhaseStatusCommand : BaseRequest
{
    public string ChallengePhaseId { get; set; } = string.Empty;
    public ChallengePhaseStatus Status { get; set; }
}

// Custom commands for voting
public class VoteChallengePostCommand : BaseRequest
{
    public string ChallengePostId { get; set; } = string.Empty;
}

public class RemoveVoteChallengePostCommand : BaseRequest
{
    public string ChallengePostId { get; set; } = string.Empty;
}
