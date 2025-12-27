using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class Challenge : BaseEntity
{
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public ChallengeStatus Status { get; set; } = ChallengeStatus.Draft;
    [DataMember] public string CreatedByUserId { get; set; } = string.Empty;
    [DataMember] public string? ImageUrl { get; set; }
    [DataMember] public string? Category { get; set; }
}

public enum ChallengeStatus
{
    Draft,
    Open,
    Closed,
    Archived
}

public class ChallengeValidator : AbstractValidator<Challenge>
{
    public ChallengeValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Challenge name is required")
            .MaximumLength(200).WithMessage("Challenge name must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Challenge description is required")
            .MaximumLength(2000).WithMessage("Challenge description must not exceed 2000 characters");
        
        RuleFor(x => x.CreatedByUserId)
            .NotEmpty().WithMessage("CreatedByUserId is required");
    }
}
