using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class Blog : BaseEntity
{
    [DataMember] public string Title { get; set; } = string.Empty;
    [DataMember] public string Content { get; set; } = string.Empty;
    [DataMember] public string Tags { get; set; } = string.Empty;
    [DataMember] public string FeaturedImageUrl { get; set; } = string.Empty;
    [DataMember] public string Abstract { get; set; } = string.Empty;
}


public class BlogValidator : AbstractValidator<Blog>
{
    public BlogValidator()
    {
        RuleFor(b => b.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(b => b.Content)
            .NotEmpty().WithMessage("Content is required.");

        RuleFor(b => b.Abstract)
            .NotEmpty().WithMessage("Abstract is required.")
            .MaximumLength(500).WithMessage("Abstract cannot exceed 500 characters.");
    }
}
