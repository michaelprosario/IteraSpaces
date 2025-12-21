using System;
using AppCore.Entities;

namespace AppCore.DTOs;

public class StoreLeanSessionNoteCommand
{
    public string? Id { get; set; }
    public string LeanSessionId { get; set; } = string.Empty;
    public string? LeanTopicId { get; set; }
    public string Content { get; set; } = string.Empty;
    public NoteType NoteType { get; set; } = NoteType.General;
    public string CreatedByUserId { get; set; } = string.Empty;
    //public string? AssignedToUserId { get; set; }
    //public DateTime? DueDate { get; set; }
    //public bool IsCompleted { get; set; } = false;
}

public class CloseSessionCommand
{
    public string SessionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
}
