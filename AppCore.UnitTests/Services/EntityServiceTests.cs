using System;
using System.Threading.Tasks;
using AppCore.Common;
using AppCore.Entities;
using AppCore.Interfaces;
using AppCore.Services;
using NSubstitute;
using NUnit.Framework;

namespace AppCore.UnitTests.Services
{
    // Test entity for use in tests
    public class TestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    [TestFixture]
    public class EntityServiceTests
    {
        private IRepository<TestEntity> _repository;
        private EntityService<TestEntity> _entityService;

        [SetUp]
        public void Setup()
        {
            _repository = Substitute.For<IRepository<TestEntity>>();
            _entityService = new EntityService<TestEntity>(_repository);
        }

        #region AddEntityAsync Tests

        [Test]
        public async Task AddEntityAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                Description = "Test Description",
                CreatedBy = "user123"
            };

            var command = new AddEntityCommand<TestEntity>(testEntity)
            {
                UserId = "user123"
            };

            _repository.RecordExists(testEntity.Id).Returns(false);
            _repository.Add(Arg.Any<TestEntity>()).Returns(callInfo => callInfo.Arg<TestEntity>());

            // Act
            var result = await _entityService.AddEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(testEntity.Id));
            Assert.That(result.Data.Name, Is.EqualTo(testEntity.Name));
            Assert.That(result.Data.CreatedBy, Is.EqualTo("user123"));
            Assert.That(result.Data.CreatedAt, Is.Not.EqualTo(default(DateTime)));
            Assert.That(result.Data.IsDeleted, Is.False);
            _repository.Received(1).Add(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task AddEntityAsync_WithNullEntity_ShouldReturnValidationError()
        {
            // Arrange
            var command = new AddEntityCommand<TestEntity>(null!)
            {
                UserId = "user123"
            };

            // Act
            var result = await _entityService.AddEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Count, Is.GreaterThan(0));
            _repository.DidNotReceive().Add(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task AddEntityAsync_WithEmptyUserId_ShouldReturnValidationError()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                CreatedBy = "user123"
            };

            var command = new AddEntityCommand<TestEntity>(testEntity)
            {
                UserId = "" // Empty UserId
            };

            // Act
            var result = await _entityService.AddEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Any(e => e.PropertyName == "UserId"), Is.True);
            _repository.DidNotReceive().Add(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task AddEntityAsync_WithEmptyEntityId_ShouldReturnValidationError()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = "", // Empty Id
                Name = "Test Entity",
                CreatedBy = "user123"
            };

            var command = new AddEntityCommand<TestEntity>(testEntity)
            {
                UserId = "user123"
            };

            // Act
            var result = await _entityService.AddEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            _repository.DidNotReceive().Add(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task AddEntityAsync_WithExistingId_ShouldReturnFailure()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                CreatedBy = "user123"
            };

            var command = new AddEntityCommand<TestEntity>(testEntity)
            {
                UserId = "user123"
            };

            _repository.RecordExists(testEntity.Id).Returns(true);

            // Act
            var result = await _entityService.AddEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_EXISTS"));
            _repository.DidNotReceive().Add(Arg.Any<TestEntity>());
        }

        #endregion

        #region UpdateEntityAsync Tests

        [Test]
        public async Task UpdateEntityAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var existingEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Old Name",
                Description = "Old Description",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "originalUser",
                IsDeleted = false
            };

            var updatedEntity = new TestEntity
            {
                Id = existingEntity.Id,
                Name = "New Name",
                Description = "New Description",
                UpdatedBy = "user123"
            };

            var command = new UpdateEntityCommand<TestEntity>(updatedEntity)
            {
                UserId = "user123"
            };

            _repository.GetById(existingEntity.Id).Returns(existingEntity);

            // Act
            var result = await _entityService.UpdateEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Name, Is.EqualTo("New Name"));
            Assert.That(result.Data.UpdatedBy, Is.EqualTo("user123"));
            Assert.That(result.Data.UpdatedAt, Is.Not.Null);
            Assert.That(result.Data.CreatedBy, Is.EqualTo("originalUser")); // Preserved
            Assert.That(result.Data.CreatedAt, Is.EqualTo(existingEntity.CreatedAt)); // Preserved
            _repository.Received(1).Update(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task UpdateEntityAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                UpdatedBy = "user123"
            };

            var command = new UpdateEntityCommand<TestEntity>(testEntity)
            {
                UserId = "user123"
            };

            _repository.GetById(testEntity.Id).Returns((TestEntity?)null);

            // Act
            var result = await _entityService.UpdateEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_NOT_FOUND"));
            _repository.DidNotReceive().Update(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task UpdateEntityAsync_WithDeletedEntity_ShouldReturnFailure()
        {
            // Arrange
            var existingEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            var updatedEntity = new TestEntity
            {
                Id = existingEntity.Id,
                Name = "Updated Name",
                UpdatedBy = "user123"
            };

            var command = new UpdateEntityCommand<TestEntity>(updatedEntity)
            {
                UserId = "user123"
            };

            _repository.GetById(existingEntity.Id).Returns(existingEntity);

            // Act
            var result = await _entityService.UpdateEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_DELETED"));
            _repository.DidNotReceive().Update(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task UpdateEntityAsync_WithNullEntity_ShouldReturnValidationError()
        {
            // Arrange
            var command = new UpdateEntityCommand<TestEntity>(null!)
            {
                UserId = "user123"
            };

            // Act
            var result = await _entityService.UpdateEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            _repository.DidNotReceive().Update(Arg.Any<TestEntity>());
        }

        #endregion

        #region StoreEntityAsync Tests

        [Test]
        public async Task StoreEntityAsync_WithNewEntity_ShouldCreateEntity()
        {
            // Arrange
            var testEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Entity",
                Description = "Test Description"
            };

            var command = new StoreEntityCommand<TestEntity>(testEntity)
            {
                UserId = "user123"
            };

            _repository.RecordExists(testEntity.Id).Returns(false);
            _repository.Add(Arg.Any<TestEntity>()).Returns(callInfo => callInfo.Arg<TestEntity>());

            // Act
            var result = await _entityService.StoreEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Message, Is.EqualTo("Entity created successfully"));
            Assert.That(result.Data.CreatedBy, Is.EqualTo("user123"));
            _repository.Received(1).Add(Arg.Any<TestEntity>());
            _repository.DidNotReceive().Update(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task StoreEntityAsync_WithExistingEntity_ShouldUpdateEntity()
        {
            // Arrange
            var existingEntity = new TestEntity
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Old Name",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedBy = "originalUser"
            };

            var updatedEntity = new TestEntity
            {
                Id = existingEntity.Id,
                Name = "New Name",
                Description = "New Description"
            };

            var command = new StoreEntityCommand<TestEntity>(updatedEntity)
            {
                UserId = "user123"
            };

            _repository.RecordExists(existingEntity.Id).Returns(true);
            _repository.GetById(existingEntity.Id).Returns(existingEntity);

            // Act
            var result = await _entityService.StoreEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Message, Is.EqualTo("Entity updated successfully"));
            Assert.That(result.Data.UpdatedBy, Is.EqualTo("user123"));
            Assert.That(result.Data.CreatedBy, Is.EqualTo("originalUser")); // Preserved
            _repository.Received(1).Update(Arg.Any<TestEntity>());
            _repository.DidNotReceive().Add(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task StoreEntityAsync_WithNullEntity_ShouldReturnValidationError()
        {
            // Arrange
            var command = new StoreEntityCommand<TestEntity>(null!)
            {
                UserId = "user123"
            };

            // Act
            var result = await _entityService.StoreEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
        }

        #endregion

        #region DeleteEntityAsync Tests

        [Test]
        public async Task DeleteEntityAsync_WithValidCommand_ShouldReturnSuccess()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var existingEntity = new TestEntity
            {
                Id = entityId,
                Name = "Test Entity",
                IsDeleted = false
            };

            var command = new DeleteEntityCommand(entityId)
            {
                UserId = "user123"
            };

            _repository.GetById(entityId).Returns(existingEntity);

            // Act
            var result = await _entityService.DeleteEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.True);
            Assert.That(existingEntity.IsDeleted, Is.True);
            Assert.That(existingEntity.DeletedBy, Is.EqualTo("user123"));
            Assert.That(existingEntity.DeletedAt, Is.Not.Null);
            _repository.Received(1).Delete(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task DeleteEntityAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var command = new DeleteEntityCommand(entityId)
            {
                UserId = "user123"
            };

            _repository.GetById(entityId).Returns((TestEntity?)null);

            // Act
            var result = await _entityService.DeleteEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_NOT_FOUND"));
            _repository.DidNotReceive().Delete(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task DeleteEntityAsync_WithAlreadyDeletedEntity_ShouldReturnFailure()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var existingEntity = new TestEntity
            {
                Id = entityId,
                Name = "Test Entity",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            var command = new DeleteEntityCommand(entityId)
            {
                UserId = "user123"
            };

            _repository.GetById(entityId).Returns(existingEntity);

            // Act
            var result = await _entityService.DeleteEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_ALREADY_DELETED"));
            _repository.DidNotReceive().Delete(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task DeleteEntityAsync_WithEmptyEntityId_ShouldReturnValidationError()
        {
            // Arrange
            var command = new DeleteEntityCommand("")
            {
                UserId = "user123"
            };

            // Act
            var result = await _entityService.DeleteEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            _repository.DidNotReceive().Delete(Arg.Any<TestEntity>());
        }

        [Test]
        public async Task DeleteEntityAsync_WithEmptyUserId_ShouldReturnValidationError()
        {
            // Arrange
            var command = new DeleteEntityCommand(Guid.NewGuid().ToString())
            {
                UserId = "" // Empty UserId
            };

            // Act
            var result = await _entityService.DeleteEntityAsync(command);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
            Assert.That(result.ValidationErrors!.Any(e => e.PropertyName == "UserId"), Is.True);
        }

        #endregion

        #region GetEntityByIdAsync Tests

        [Test]
        public async Task GetEntityByIdAsync_WithValidId_ShouldReturnEntity()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var testEntity = new TestEntity
            {
                Id = entityId,
                Name = "Test Entity",
                Description = "Test Description",
                IsDeleted = false
            };

            var query = new GetEntityByIdQuery(entityId);

            _repository.GetById(entityId).Returns(testEntity);

            // Act
            var result = await _entityService.GetEntityByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.Not.Null);
            Assert.That(result.Data.Id, Is.EqualTo(entityId));
            Assert.That(result.Data.Name, Is.EqualTo("Test Entity"));
        }

        [Test]
        public async Task GetEntityByIdAsync_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var query = new GetEntityByIdQuery(entityId);

            _repository.GetById(entityId).Returns((TestEntity?)null);

            // Act
            var result = await _entityService.GetEntityByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_NOT_FOUND"));
        }

        [Test]
        public async Task GetEntityByIdAsync_WithDeletedEntity_ShouldReturnFailure()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            var testEntity = new TestEntity
            {
                Id = entityId,
                Name = "Test Entity",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow
            };

            var query = new GetEntityByIdQuery(entityId);

            _repository.GetById(entityId).Returns(testEntity);

            // Act
            var result = await _entityService.GetEntityByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ENTITY_DELETED"));
        }

        [Test]
        public async Task GetEntityByIdAsync_WithEmptyId_ShouldReturnValidationError()
        {
            // Arrange
            var query = new GetEntityByIdQuery("");

            // Act
            var result = await _entityService.GetEntityByIdAsync(query);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ValidationErrors, Is.Not.Null);
        }

        #endregion

        #region EntityExistsAsync Tests

        [Test]
        public async Task EntityExistsAsync_WithExistingEntity_ShouldReturnTrue()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            _repository.RecordExists(entityId).Returns(true);

            // Act
            var result = await _entityService.EntityExistsAsync(entityId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.True);
        }

        [Test]
        public async Task EntityExistsAsync_WithNonExistentEntity_ShouldReturnFalse()
        {
            // Arrange
            var entityId = Guid.NewGuid().ToString();
            _repository.RecordExists(entityId).Returns(false);

            // Act
            var result = await _entityService.EntityExistsAsync(entityId);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Data, Is.False);
        }

        [Test]
        public async Task EntityExistsAsync_WithEmptyId_ShouldReturnFailure()
        {
            // Arrange
            var entityId = "";

            // Act
            var result = await _entityService.EntityExistsAsync(entityId);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_INPUT"));
        }

        [Test]
        public async Task EntityExistsAsync_WithNullId_ShouldReturnFailure()
        {
            // Arrange
            string? entityId = null;

            // Act
            var result = await _entityService.EntityExistsAsync(entityId!);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INVALID_INPUT"));
        }

        #endregion
    }
}
