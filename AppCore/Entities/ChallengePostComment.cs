using System;
using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class ChallengePostComment : BaseEntity
{
    [DataMember] public string ChallengePostId { get; set; } = string.Empty;
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string? ParentCommentId { get; set; } // For threaded comments
    [DataMember] public CommentStatus Status { get; set; } = CommentStatus.Active;
}

public enum CommentStatus
{
    Active,
    Edited,
    Deleted,
    Flagged
}

public class ChallengePostCommentValidator : AbstractValidator<ChallengePostComment>
{
    public ChallengePostCommentValidator()
    {
        RuleFor(x => x.ChallengePostId)
            .NotEmpty().WithMessage("ChallengePostId is required");
        
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");
        
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Comment content is required")
            .MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");
    }
}
