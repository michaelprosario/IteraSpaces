using AppCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppInfra.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.Id)
                    .IsRequired()
                    .HasMaxLength(36);

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.DisplayName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirebaseUid)
                    .IsRequired()
                    .HasMaxLength(128);

                entity.HasIndex(e => e.FirebaseUid)
                    .IsUnique();

                entity.Property(e => e.EmailVerified)
                    .IsRequired();

                entity.Property(e => e.ProfilePhotoUrl)
                    .HasMaxLength(500);

                entity.Property(e => e.Bio)
                    .HasMaxLength(1000);

                entity.Property(e => e.Location)
                    .HasMaxLength(255);

                // Store collections as JSON
                entity.Property(e => e.Skills)
                    .HasColumnType("jsonb");

                entity.Property(e => e.Interests)
                    .HasColumnType("jsonb");

                entity.Property(e => e.AreasOfExpertise)
                    .HasColumnType("jsonb");

                entity.Property(e => e.SocialLinks)
                    .HasColumnType("jsonb");

                // Store UserPrivacySettings as JSON
                entity.OwnsOne(e => e.PrivacySettings, privacySettings =>
                {
                    privacySettings.ToJson();
                });

                entity.Property(e => e.Status)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(e => e.LastLoginAt);

                // BaseEntity properties
                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.CreatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.UpdatedAt);

                entity.Property(e => e.UpdatedBy)
                    .HasMaxLength(255);

                entity.Property(e => e.IsDeleted)
                    .IsRequired();

                entity.Property(e => e.DeletedAt);

                entity.Property(e => e.DeletedBy)
                    .HasMaxLength(255);

                // Filter out soft-deleted entities by default
                entity.HasQueryFilter(e => !e.IsDeleted);
            });
        }
    }
}
