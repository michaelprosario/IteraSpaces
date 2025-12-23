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
    public LeanSession? Session { get; set; }
    public List<LeanTopic> Topics { get; set; } = new();
    public List<LeanParticipant> Participants { get; set; } = new();
    public List<LeanSessionNote> Notes { get; set; } = new();
    public List<LeanTopicVote> Votes { get; set; } = new();
    public List<User> Users { get; set; } = new();
}
