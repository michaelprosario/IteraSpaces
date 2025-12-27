using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePhase : BaseEntity
{
    [DataMember] public string ChallengeId { get; set; } = string.Empty;
    [DataMember] public string Name { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public ChallengePhaseStatus Status { get; set; } = ChallengePhaseStatus.Planned;
    [DataMember] public DateTime? StartDate { get; set; }
    [DataMember] public DateTime? EndDate { get; set; }
    [DataMember] public int DisplayOrder { get; set; } = 0;
}

public enum ChallengePhaseStatus
{
    Planned,
    Open,
    Closed,
    Archived
}

public class ChallengePhaseValidator : AbstractValidator<ChallengePhase>
{
    public ChallengePhaseValidator()
    {
        RuleFor(x => x.ChallengeId)
            .NotEmpty().WithMessage("ChallengeId is required");
        
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Phase name is required")
            .MaximumLength(200).WithMessage("Phase name must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Phase description is required")
            .MaximumLength(2000).WithMessage("Phase description must not exceed 2000 characters");
        
        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.StartDate)
            .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
            .WithMessage("End date must be after start date");
    }
}
