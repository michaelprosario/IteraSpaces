using System;
using System.Collections.Generic;
using AppCore.Services;
using AppCore.Entities;

namespace AppCore.DTOs;

public class GetLeanSessionsQuery : SearchQuery
{
    public SessionStatus? Status { get; set; }
    public string? FacilitatorUserId { get; set; }
}

public class GetLeanSessionQuery
{
    public string SessionId { get; set; } = string.Empty;
}

public class GetLeanSessionResult
{
    public string SessionName { get; set; } = string.Empty;
    public LeanTopic? CurrentTopic { get; set; }
    public List<LeanTopic> TopicBacklog { get; set; } = new();
    public List<LeanTopicVote> TopicVotes { get; set; } = new();
    public List<User> Users { get; set; } = new();
}
