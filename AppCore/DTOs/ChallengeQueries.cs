using System.Collections.Generic;
using AppCore.Common;
using AppCore.Entities;
using AppCore.Services;

namespace AppCore.DTOs;

// Challenge Queries
public class GetChallengesQuery : SearchQuery
{
    public ChallengeStatus? Status { get; set; }
    public string? Category { get; set; }
    public string? CreatedByUserId { get; set; }
}

public class GetChallengeQuery
{
    public string ChallengeId { get; set; } = string.Empty;
}

public class GetChallengeResult
{
    public Challenge Challenge { get; set; } = null!;
    public List<ChallengePhase> Phases { get; set; } = new();
    public int TotalPosts { get; set; }
}

// ChallengePhase Queries
public class GetChallengePhasesQuery
{
    public string? ChallengeId { get; set; }
    public ChallengePhaseStatus? Status { get; set; }
}

public class GetChallengePhaseQuery
{
    public string ChallengePhaseId { get; set; } = string.Empty;
}

public class GetChallengePhaseResult
{
    public ChallengePhase Phase { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public int PostCount { get; set; }
}

// ChallengePost Queries
public class GetChallengePostsQuery : SearchQuery
{
    public string? ChallengePhaseId { get; set; }
    public string? ChallengeId { get; set; }
    public string? SubmittedByUserId { get; set; }
    public List<string>? Tags { get; set; }
    public string? SortBy { get; set; } = "votes"; // votes, recent, comments
}

public class GetChallengePostQuery
{
    public string ChallengePostId { get; set; } = string.Empty;
    public string? RequestingUserId { get; set; } // To check if user has voted
}

public class GetChallengePostResult
{
    public ChallengePost Post { get; set; } = null!;
    public ChallengePhase Phase { get; set; } = null!;
    public Challenge Challenge { get; set; } = null!;
    public string SubmittedByUsername { get; set; } = string.Empty;
    public bool HasUserVoted { get; set; }
    public List<ChallengePostComment> Comments { get; set; } = new();
}

// Comment Queries
public class GetChallengePostCommentsQuery
{
    public string ChallengePostId { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
