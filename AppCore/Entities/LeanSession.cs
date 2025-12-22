using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class LeanSession : BaseEntity
{
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    [DataMember] public DateTime? ScheduledStartTime { get; set; }
    [DataMember] public DateTime? ActualStartTime { get; set; }
    [DataMember] public DateTime? ActualEndTime { get; set; }
    [DataMember] public string FacilitatorUserId { get; set; } = string.Empty;
    [DataMember] public int DefaultTopicDuration { get; set; } = 7; // minutes
    [DataMember] public bool IsPublic { get; set; } = true;
    [DataMember] public string? InviteCode { get; set; }
}

public enum SessionStatus
{
    Scheduled,
    InProgress,
    Completed,
    Archived,
    Cancelled
}

public class LeanSessionValidator : AbstractValidator<LeanSession>
{
    public LeanSessionValidator()
    {
        RuleFor(s => s.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(s => s.FacilitatorUserId)
            .NotEmpty().WithMessage("Facilitator user ID is required.");

        RuleFor(s => s.DefaultTopicDuration)
            .GreaterThan(0).WithMessage("Default topic duration must be greater than 0.");
    }
}
