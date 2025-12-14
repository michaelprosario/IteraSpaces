# Entity Framework Core Setup with PostgreSQL

This document explains how Entity Framework Core has been configured in the IteraSpaces project and provides guidance on working with EF Core migrations.

## Overview

The project uses Entity Framework Core 10.0.0 with the Npgsql provider to connect to a PostgreSQL database. The database is hosted in a Docker container and accessed through the `ApplicationDbContext` class.

## Project Structure

### Packages

The following NuGet packages have been added:

**IteraWebApi Project:**
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0) - PostgreSQL provider for EF Core
- `Microsoft.EntityFrameworkCore.Design` (10.0.0) - Design-time tools for EF Core migrations

**AppInfra Project:**
- `Microsoft.EntityFrameworkCore` (10.0.0) - Core EF functionality
- `Npgsql.EntityFrameworkCore.PostgreSQL` (10.0.0) - PostgreSQL provider

### DbContext Configuration

The `ApplicationDbContext` class is located at [AppInfra/Data/ApplicationDbContext.cs](AppInfra/Data/ApplicationDbContext.cs) and includes:

- Entity configuration for the `User` entity
- JSON column storage for collections (Skills, Interests, AreasOfExpertise, SocialLinks)
- Owned entity configuration for `UserPrivacySettings`
- Soft delete query filter (automatically excludes soft-deleted records)
- Unique indexes on Email and FirebaseUid

### Connection String

The connection string is configured in `appsettings.json` and `appsettings.Development.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=iteraspaces;Username=postgres;Password=Foobar321"
}
```

### Program.cs Configuration

The DbContext is registered in [IteraWebApi/Program.cs](IteraWebApi/Program.cs):

```csharp
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("IteraWebApi")
    ));
```

**Important:** The `MigrationsAssembly` is set to `"IteraWebApi"` because migrations are stored in the Web API project, even though the DbContext lives in the AppInfra project.

## Prerequisites

### 1. Install EF Core Tools

EF Core tools are required for creating and managing migrations:

```bash
dotnet tool install --global dotnet-ef
```

Verify installation:
```bash
dotnet ef --version
```

### 2. PostgreSQL Database

Ensure the PostgreSQL Docker container is running:

```bash
cd DockerCompose/Postgres
docker-compose up -d
```

Verify the container is running:
```bash
docker ps | grep postgres
```

## Working with Migrations

### Creating a Migration

When you modify entity classes or add new entities, create a migration to capture the schema changes:

```bash
cd IteraWebApi
dotnet ef migrations add MigrationName
```

**Naming Convention:** Use descriptive names in PascalCase:
- `InitialCreate` - First migration
- `AddUserProfile` - Adding user profile fields
- `CreateProjectEntity` - Adding a new entity
- `UpdateUserStatusEnum` - Modifying existing schema

### Applying Migrations

Apply pending migrations to the database:

```bash
cd IteraWebApi
dotnet ef database update
```

This command:
1. Creates the database if it doesn't exist
2. Creates the `__EFMigrationsHistory` table to track applied migrations
3. Executes all pending migrations in order

### Rolling Back Migrations

To revert to a specific migration:

```bash
cd IteraWebApi
dotnet ef database update PreviousMigrationName
```

To revert all migrations:

```bash
cd IteraWebApi
dotnet ef database update 0
```

### Removing a Migration

If you created a migration but haven't applied it yet:

```bash
cd IteraWebApi
dotnet ef migrations remove
```

**Note:** You can only remove the most recent unapplied migration.

### Listing Migrations

View all migrations and their status:

```bash
cd IteraWebApi
dotnet ef migrations list
```

### Generating SQL Scripts

Generate SQL for a migration without applying it:

```bash
cd IteraWebApi
dotnet ef migrations script
```

Generate SQL for a specific range:

```bash
cd IteraWebApi
dotnet ef migrations script FromMigration ToMigration
```

## Common Scenarios

### Adding a New Entity

1. Create the entity class in [AppCore/Entities](AppCore/Entities)
2. Add a DbSet property to [ApplicationDbContext](AppInfra/Data/ApplicationDbContext.cs):
   ```csharp
   public DbSet<MyEntity> MyEntities { get; set; }
   ```
3. Configure the entity in `OnModelCreating`:
   ```csharp
   modelBuilder.Entity<MyEntity>(entity =>
   {
       entity.ToTable("MyEntities");
       entity.HasKey(e => e.Id);
       // Add more configuration...
   });
   ```
4. Create and apply the migration:
   ```bash
   cd IteraWebApi
   dotnet ef migrations add AddMyEntity
   dotnet ef database update
   ```

### Modifying an Existing Entity

1. Update the entity class in [AppCore/Entities](AppCore/Entities)
2. Update the configuration in [ApplicationDbContext](AppInfra/Data/ApplicationDbContext.cs) if needed
3. Create and apply the migration:
   ```bash
   cd IteraWebApi
   dotnet ef migrations add UpdateMyEntity
   dotnet ef database update
   ```

### Resetting the Database

To drop the database and recreate it:

```bash
cd IteraWebApi
dotnet ef database drop
dotnet ef database update
```

## PostgreSQL-Specific Features

### JSONB Columns

The project uses PostgreSQL's `jsonb` type for storing collections and complex objects:

```csharp
entity.Property(e => e.Skills)
    .HasColumnType("jsonb");
```

This allows:
- Efficient storage of arrays and dictionaries
- Indexing and querying JSON data
- Schema flexibility for semi-structured data

### Owned Entities (JSON Documents)

Complex types like `UserPrivacySettings` are stored as JSON using the `.ToJson()` method:

```csharp
entity.OwnsOne(e => e.PrivacySettings, privacySettings =>
{
    privacySettings.ToJson();
});
```

## Troubleshooting

### Migration Command Not Found

If you get "command not found" when running `dotnet ef`:
```bash
dotnet tool install --global dotnet-ef
```

### Connection Errors

If you get authentication errors:
1. Check the connection string in `appsettings.json`
2. Verify PostgreSQL is running: `docker ps | grep postgres`
3. Check credentials match the Docker compose file at [DockerCompose/Postgres/docker-compose.yaml](DockerCompose/Postgres/docker-compose.yaml)

### Migration Already Applied

If you try to apply a migration that's already been applied:
```bash
cd IteraWebApi
dotnet ef migrations list
```

Check the status and apply only pending migrations.

### Build Errors During Migrations

Ensure the project builds successfully before creating migrations:
```bash
cd IteraWebApi
dotnet build
```

## Best Practices

1. **Always test migrations** in development before applying to production
2. **Use descriptive migration names** that explain the change
3. **Review generated migration code** before applying - EF might not detect all changes correctly
4. **Backup production databases** before applying migrations
5. **Keep migrations small and focused** - one logical change per migration
6. **Don't modify applied migrations** - create a new migration to fix issues
7. **Use transactions** - EF Core migrations run in a transaction by default
8. **Version control migrations** - commit migration files to source control

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [Npgsql Entity Framework Core Provider](https://www.npgsql.org/efcore/)
- [EF Core Migrations Overview](https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [PostgreSQL JSON Types](https://www.postgresql.org/docs/current/datatype-json.html)
