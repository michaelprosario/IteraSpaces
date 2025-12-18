using System.Runtime.Serialization;
using FluentValidation;

namespace AppCore.Entities;

[DataContract]
public class UserLoginEvent : BaseEntity
{
    [DataMember] public string UserId { get; set; } = string.Empty;
    [DataMember] public string? UserAgent { get; set; } = string.Empty;
}

public class UserLoginEventValidator : AbstractValidator<UserLoginEvent>
{
    public UserLoginEventValidator()
    {
        RuleFor(e => e.UserId)
            .NotEmpty().WithMessage("UserId is required.");        
    }
}   