using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class LeanSessionNote : BaseEntity
{
    [DataMember] public string LeanSessionId { get; set; } = string.Empty;
    [DataMember] public string? LeanTopicId { get; set; }
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public NoteType NoteType { get; set; } = NoteType.General;
    [DataMember] public string CreatedByUserId { get; set; } = string.Empty;
    //[DataMember] public string? AssignedToUserId { get; set; }
    //[DataMember] public DateTime? DueDate { get; set; }
    //[DataMember] public bool IsCompleted { get; set; } = false;
}

public enum NoteType
{
    General,
    ActionItem,
    Decision,
    KeyPoint
}

public class LeanSessionNoteValidator : AbstractValidator<LeanSessionNote>
{
    public LeanSessionNoteValidator()
    {
        RuleFor(n => n.LeanSessionId)
            .NotEmpty().WithMessage("Lean session ID is required.");

        RuleFor(n => n.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(n => n.CreatedByUserId)
            .NotEmpty().WithMessage("Created by user ID is required.");
    }
}
