using AppCore.Entities;

namespace AppCore.DTOs;

public class JoinSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
}

public class LeaveSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}

public class GetActiveParticipantsQuery
{
    public string SessionId { get; set; } = string.Empty;
}

public class RemoveVoteCommand
{
    public string TopicId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}

public class ChangeSessionStatusCommand
{
    public string SessionId { get; set; } = string.Empty;
    public SessionStatus NewStatus { get; set; }
}
