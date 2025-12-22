using System.Diagnostics;
using AppCore.Entities;
using FluentValidation;

namespace AppCore.Services;

public interface IUsersQueryService
{
    Task<PagedResults<User>> GetUsersAsync(SearchQuery query); 
}

public interface IUsersQueryRepository
{
    Task<PagedResults<User>> GetUsersAsync(SearchQuery query);     
}


public class UsersQueryService : IUsersQueryService
{
    private readonly IUsersQueryRepository _repository;     

    public UsersQueryService(IUsersQueryRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResults<User>> GetUsersAsync(SearchQuery query)
    {
        var validator = new SearchQueryValidator();
        var validationResult = await validator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        return await _repository.GetUsersAsync(query);
    }
}   