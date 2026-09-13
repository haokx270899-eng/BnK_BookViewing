using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyViewing.Domain.Entities;

namespace PropertyViewing.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Address)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(x => x.TimeZoneId)
            .HasMaxLength(50)
            .HasDefaultValue("Europe/London")
            .IsRequired();

        builder.HasData(
            new Property
            {
                Id = 1,
                Address = "1 Main Street",
                TimeZoneId = "Europe/London"
            },
            new Property
            {
                Id = 2,
                Address = "2 High Street",
                TimeZoneId = "America/New_York"
            },
            new Property
            {
                Id = 3,
                Address = "3 Park Avenue",
                TimeZoneId = "Asia/Ho_Chi_Minh"
            }
        );
    }
}

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Email)
            .HasMaxLength(320);

        builder.HasData(
            new User { Id = 1, Name = "User 1", Email = "user1@example.test" },
            new User { Id = 2, Name = "User 2", Email = "user2@example.test" },
            new User { Id = 3, Name = "User 3", Email = "user3@example.test" }
        );
    }
}

public sealed class ViewingConfiguration : IEntityTypeConfiguration<Viewing>
{
    public void Configure(EntityTypeBuilder<Viewing> builder)
    {
        builder.ToTable("viewings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.StartTime)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.EndTime)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => new { x.PropertyId, x.StartTime }).IsUnique();
        builder.HasIndex(x => x.StartTime);

        builder.HasOne(x => x.Property)
            .WithMany(x => x.Viewings)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Viewings)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}