using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePost : BaseEntity
{
    [DataMember] public string ChallengePhaseId { get; set; } = string.Empty;
    [DataMember] public string SubmittedByUserId { get; set; } = string.Empty;
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Description { get; set; } = string.Empty;
    [DataMember] public string? ImageUrl { get; set; }
    [DataMember] public List<string> Tags { get; set; } = new();
    [DataMember] public int VoteCount { get; set; } = 0;
    [DataMember] public int CommentCount { get; set; } = 0;
    [DataMember] public ChallengePostStatus Status { get; set; } = ChallengePostStatus.Active;
}

public enum ChallengePostStatus
{
    Active,
    Archived,
    Flagged
}

public class ChallengePostValidator : AbstractValidator<ChallengePost>
{
    public ChallengePostValidator()
    {
        RuleFor(x => x.ChallengePhaseId)
            .NotEmpty().WithMessage("ChallengePhaseId is required");
        
        RuleFor(x => x.SubmittedByUserId)
            .NotEmpty().WithMessage("SubmittedByUserId is required");
        
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Post title is required")
            .MaximumLength(200).WithMessage("Post title must not exceed 200 characters");
        
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Post description is required")
            .MaximumLength(5000).WithMessage("Post description must not exceed 5000 characters");
    }
}
