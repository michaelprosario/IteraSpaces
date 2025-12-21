using AppCore.Entities;

namespace AppCore.DTOs;

public class VoteForLeanTopicCommand
{
    public string LeanTopicId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string LeanSessionId { get; set; } = string.Empty;
}

public class SetTopicStatusCommand
{
    public string TopicId { get; set; } = string.Empty;
    public TopicStatus Status { get; set; }
    public string UserId { get; set; } = string.Empty;
}
