using FluentValidation;

namespace AppCore.Services;

// create fluent validator for SearchQuery  
public class SearchQueryValidator : FluentValidation.AbstractValidator<SearchQuery>
{
    public SearchQueryValidator()
    {
        RuleFor(x => x.PageNumber).GreaterThan(0).WithMessage("PageNumber must be greater than 0");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("PageSize must be between 1 and 100");
    }
}
