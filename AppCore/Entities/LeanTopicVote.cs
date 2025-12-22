using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class LeanTopicVote : BaseEntity
{
    [DataMember] public string LeanTopicId { get; set; } = string.Empty;
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public string LeanSessionId { get; set; } = string.Empty;
    [DataMember] public DateTime VotedAt { get; set; }
}

public class LeanTopicVoteValidator : AbstractValidator<LeanTopicVote>
{
    public LeanTopicVoteValidator()
    {
        RuleFor(v => v.LeanTopicId)
            .NotEmpty().WithMessage("Lean topic ID is required.");

        RuleFor(v => v.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(v => v.LeanSessionId)
            .NotEmpty().WithMessage("Lean session ID is required.");

        RuleFor(v => v.VotedAt)
            .NotEmpty().WithMessage("Voted at timestamp is required.");
    }
}
