using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class LeanParticipant : BaseEntity
{
    [DataMember] public string LeanSessionId { get; set; } = string.Empty;
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public ParticipantRole Role { get; set; } = ParticipantRole.Participant;
    [DataMember] public DateTime JoinedAt { get; set; }
    [DataMember] public DateTime? LeftAt { get; set; }
    [DataMember] public bool IsActive { get; set; } = true;
    
    // FCM-related fields
    [DataMember] public bool IsSubscribedToFCM { get; set; }
    [DataMember] public DateTime? FCMSubscribedAt { get; set; }
}

public enum ParticipantRole
{
    Facilitator,
    Participant,
    Observer
}

public class LeanParticipantValidator : AbstractValidator<LeanParticipant>
{
    public LeanParticipantValidator()
    {
        RuleFor(p => p.LeanSessionId)
            .NotEmpty().WithMessage("Lean session ID is required.");

        RuleFor(p => p.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(p => p.JoinedAt)
            .NotEmpty().WithMessage("Joined at timestamp is required.");
    }
}
