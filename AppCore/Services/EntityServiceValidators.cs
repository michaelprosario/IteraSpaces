using AppCore.Entities;
using FluentValidation;

namespace AppCore.Services;

public class AddEntityCommandValidator<TEntity> : AbstractValidator<AddEntityCommand<TEntity>> where TEntity : BaseEntity
{
    public AddEntityCommandValidator()
    {
        RuleFor(x => x.Entity)
            .NotNull()
            .WithMessage("Entity cannot be null");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        When(x => x.Entity != null, () =>
        {
            RuleFor(x => x.Entity.Id)
                .NotEmpty()
                .WithMessage("Entity Id is required");

            RuleFor(x => x.Entity.CreatedBy)
                .NotEmpty()
                .WithMessage("CreatedBy is required");
        });
    }
}

public class UpdateEntityCommandValidator<TEntity> : AbstractValidator<UpdateEntityCommand<TEntity>> where TEntity : BaseEntity
{
    public UpdateEntityCommandValidator()
    {
        RuleFor(x => x.Entity)
            .NotNull()
            .WithMessage("Entity cannot be null");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        When(x => x.Entity != null, () =>
        {
            RuleFor(x => x.Entity.Id)
                .NotEmpty()
                .WithMessage("Entity Id is required");

            RuleFor(x => x.Entity.UpdatedBy)
                .NotEmpty()
                .WithMessage("UpdatedBy is required");
        });
    }
}

public class StoreEntityCommandValidator<TEntity> : AbstractValidator<StoreEntityCommand<TEntity>> where TEntity : BaseEntity
{
    public StoreEntityCommandValidator()
    {
        RuleFor(x => x.Entity)
            .NotNull()
            .WithMessage("Entity cannot be null");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");

        When(x => x.Entity != null, () =>
        {
            RuleFor(x => x.Entity.Id)
                .NotEmpty()
                .WithMessage("Entity Id is required");
        });
    }
}

public class DeleteEntityCommandValidator : AbstractValidator<DeleteEntityCommand>
{
    public DeleteEntityCommandValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithMessage("EntityId is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}

public class GetEntityByIdQueryValidator : AbstractValidator<GetEntityByIdQuery>
{
    public GetEntityByIdQueryValidator()
    {
        RuleFor(x => x.EntityId)
            .NotEmpty()
            .WithMessage("EntityId is required");
    }
}
