

using AppCore.Common;
using AppCore.Entities;
using AppCore.Interfaces;

namespace AppCore.Services;

// create command to add entity; it should be generic that it uses IEntityRepository<TEntity> where TEntity : BaseEntity

public class BaseRequest
{
    public string UserId { get; set; } = string.Empty;
}

public class AddEntityCommand<TEntity> : BaseRequest where TEntity : BaseEntity
{
    public TEntity Entity { get; set; }

    public AddEntityCommand(TEntity entity)
    {
        Entity = entity;
    }
}

public class UpdateEntityCommand<TEntity> : BaseRequest where TEntity : BaseEntity
{
    public TEntity Entity { get; set; }

    public UpdateEntityCommand(TEntity entity)
    {
        Entity = entity;
    }
}   

public class StoreEntityCommand<TEntity> : BaseRequest where TEntity : BaseEntity
{
    public TEntity Entity { get; set; }

    public StoreEntityCommand(TEntity entity)
    {
        Entity = entity;
    }
}

public class DeleteEntityCommand : BaseRequest
{
    public string EntityId { get; set; }

    public DeleteEntityCommand(string entityId)
    {
        EntityId = entityId;
    }
}

public class GetEntityByIdQuery : BaseRequest
{
    public string EntityId { get; set; }

    public GetEntityByIdQuery(string entityId)
    {
        EntityId = entityId;
    }
}

public interface IEntityService<TEntity> where TEntity : BaseEntity
{
    Task<AppResult<TEntity>> AddEntityAsync(AddEntityCommand<TEntity> command);
    Task<AppResult<TEntity>> UpdateEntityAsync(UpdateEntityCommand<TEntity> command);
    Task<AppResult<TEntity>> StoreEntityAsync(StoreEntityCommand<TEntity> command);
    Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command);
    Task<AppResult<TEntity>> GetEntityByIdAsync(GetEntityByIdQuery query);
    Task<AppResult<bool>> EntityExistsAsync(string entityId);
}

public class EntityService<TEntity> : IEntityService<TEntity> where TEntity : BaseEntity
{
    private readonly IRepository<TEntity> _repository;
    private readonly AddEntityCommandValidator<TEntity> _addValidator;
    private readonly UpdateEntityCommandValidator<TEntity> _updateValidator;
    private readonly StoreEntityCommandValidator<TEntity> _storeValidator;
    private readonly DeleteEntityCommandValidator _deleteValidator;
    private readonly GetEntityByIdQueryValidator _getByIdValidator;

    public EntityService(IRepository<TEntity> repository)
    {
        _repository = repository;
        _addValidator = new AddEntityCommandValidator<TEntity>();
        _updateValidator = new UpdateEntityCommandValidator<TEntity>();
        _storeValidator = new StoreEntityCommandValidator<TEntity>();
        _deleteValidator = new DeleteEntityCommandValidator();
        _getByIdValidator = new GetEntityByIdQueryValidator();
    }

    public async Task<AppResult<TEntity>> AddEntityAsync(AddEntityCommand<TEntity> command)
    {
        // Validate command
        var validationResult = await _addValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<TEntity>.ValidationFailure(errors);
        }

        // Check if entity already exists
        if (await _repository.RecordExists(command.Entity.Id))
        {
            return AppResult<TEntity>.FailureResult(
                $"Entity with Id '{command.Entity.Id}' already exists",
                "ENTITY_EXISTS");
        }

        // Set audit fields
        command.Entity.CreatedAt = DateTime.UtcNow;
        command.Entity.CreatedBy = command.UserId;
        command.Entity.IsDeleted = false;

        // Add entity
        var addedEntity = await _repository.Add(command.Entity);

        return AppResult<TEntity>.SuccessResult(
            addedEntity,
            "Entity added successfully");
    }

    public async Task<AppResult<TEntity>> UpdateEntityAsync(UpdateEntityCommand<TEntity> command)
    {
        // Validate command
        var validationResult = await _updateValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<TEntity>.ValidationFailure(errors);
        }

        // Check if entity exists
        var existingEntity = await _repository.GetById(command.Entity.Id);
        if (existingEntity == null)
        {
            return AppResult<TEntity>.FailureResult(
                $"Entity with Id '{command.Entity.Id}' not found",
                "ENTITY_NOT_FOUND");
        }

        // Check if entity is deleted
        if (existingEntity.IsDeleted)
        {
            return AppResult<TEntity>.FailureResult(
                "Cannot update a deleted entity",
                "ENTITY_DELETED");
        }

        // Set audit fields
        command.Entity.UpdatedAt = DateTime.UtcNow;
        command.Entity.UpdatedBy = command.UserId;

        // Preserve creation fields
        command.Entity.CreatedAt = existingEntity.CreatedAt;
        command.Entity.CreatedBy = existingEntity.CreatedBy;
        command.Entity.IsDeleted = existingEntity.IsDeleted;
        command.Entity.DeletedAt = existingEntity.DeletedAt;
        command.Entity.DeletedBy = existingEntity.DeletedBy;

        // Update entity
        await _repository.Update(command.Entity);

        return AppResult<TEntity>.SuccessResult(
            command.Entity,
            "Entity updated successfully");
    }

    public async Task<AppResult<TEntity>> StoreEntityAsync(StoreEntityCommand<TEntity> command)
    {
        // Validate command
        var validationResult = await _storeValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<TEntity>.ValidationFailure(errors);
        }

        // Check if entity exists
        var exists = await _repository.RecordExists(command.Entity.Id);

        if (exists)
        {
            // Update existing entity
            var existingEntity = await _repository.GetById(command.Entity.Id);
            if (existingEntity == null)
            {
                return AppResult<TEntity>.FailureResult(
                    $"Entity with Id '{command.Entity.Id}' not found",
                    "ENTITY_NOT_FOUND");
            }

            // Set audit fields
            command.Entity.UpdatedAt = DateTime.UtcNow;
            command.Entity.UpdatedBy = command.UserId;

            // Preserve creation fields
            command.Entity.CreatedAt = existingEntity.CreatedAt;
            command.Entity.CreatedBy = existingEntity.CreatedBy;
            command.Entity.IsDeleted = existingEntity.IsDeleted;
            command.Entity.DeletedAt = existingEntity.DeletedAt;
            command.Entity.DeletedBy = existingEntity.DeletedBy;

            await _repository.Update(command.Entity);

            return AppResult<TEntity>.SuccessResult(
                command.Entity,
                "Entity updated successfully");
        }
        else
        {
            // Add new entity
            command.Entity.CreatedAt = DateTime.UtcNow;
            command.Entity.CreatedBy = command.UserId;
            command.Entity.IsDeleted = false;

            var addedEntity = await _repository.Add(command.Entity);

            return AppResult<TEntity>.SuccessResult(
                addedEntity,
                "Entity created successfully");
        }
    }

    public async Task<AppResult<bool>> DeleteEntityAsync(DeleteEntityCommand command)
    {
        // Validate command
        var validationResult = await _deleteValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<bool>.ValidationFailure(errors);
        }

        // Check if entity exists
        var entity = await _repository.GetById(command.EntityId);
        if (entity == null)
        {
            return AppResult<bool>.FailureResult(
                $"Entity with Id '{command.EntityId}' not found",
                "ENTITY_NOT_FOUND");
        }

        // Check if already deleted
        if (entity.IsDeleted)
        {
            return AppResult<bool>.FailureResult(
                "Entity is already deleted",
                "ENTITY_ALREADY_DELETED");
        }

        // Soft delete
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedBy = command.UserId;

        await _repository.Delete(entity);

        return AppResult<bool>.SuccessResult(
            true,
            "Entity deleted successfully");
    }

    public async Task<AppResult<TEntity>> GetEntityByIdAsync(GetEntityByIdQuery query)
    {
        // Validate query
        var validationResult = await _getByIdValidator.ValidateAsync(query);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => new ValidationError
            {
                PropertyName = e.PropertyName,
                ErrorMessage = e.ErrorMessage
            }).ToList();
            return AppResult<TEntity>.ValidationFailure(errors);
        }

        // Get entity
        var entity = await _repository.GetById(query.EntityId);
        if (entity == null)
        {
            return AppResult<TEntity>.FailureResult(
                $"Entity with Id '{query.EntityId}' not found",
                "ENTITY_NOT_FOUND");
        }

        // Check if deleted
        if (entity.IsDeleted)
        {
            return AppResult<TEntity>.FailureResult(
                "Entity has been deleted",
                "ENTITY_DELETED");
        }

        return AppResult<TEntity>.SuccessResult(entity);
    }

    public async Task<AppResult<bool>> EntityExistsAsync(string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            return AppResult<bool>.FailureResult(
                "EntityId is required",
                "INVALID_INPUT");
        }

        var exists = await _repository.RecordExists(entityId);

        return AppResult<bool>.SuccessResult(exists);
    }
} 

