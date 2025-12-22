using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class LeanTopic : BaseEntity
{
    [DataMember] public string LeanSessionId { get; set; } = string.Empty;
    [DataMember] public string SubmittedByUserId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string? Description { get; set; }
    //[DataMember] public string? Category { get; set; }
    [DataMember] public TopicStatus Status { get; set; } = TopicStatus.ToDiscuss;
    [DataMember] public int VoteCount { get; set; } = 0;
    [DataMember] public int DisplayOrder { get; set; } = 0;
    [DataMember] public DateTime? DiscussionStartedAt { get; set; }
    [DataMember] public DateTime? DiscussionEndedAt { get; set; }
    [DataMember] public bool IsAnonymous { get; set; } = false;
}

public enum TopicStatus
{
    ToDiscuss,
    Discussing,
    Discussed,
    Archived
}

public class LeanTopicValidator : AbstractValidator<LeanTopic>
{
    public LeanTopicValidator()
    {
        RuleFor(t => t.LeanSessionId)
            .NotEmpty().WithMessage("Lean session ID is required.");

        RuleFor(t => t.SubmittedByUserId)
            .NotEmpty().WithMessage("Submitted by user ID is required.");

        RuleFor(t => t.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");
    }
}
