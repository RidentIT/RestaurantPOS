using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class UserModulePermissionConfiguration : IEntityTypeConfiguration<UserModulePermission>
{
    public void Configure(EntityTypeBuilder<UserModulePermission> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("UserModulePermissions");

        // Composite key: a user can hold a given module at most once.
        builder.HasKey(p => new { p.UserId, p.Module });

        builder.Property(p => p.Module)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.GrantedAtUtc).IsRequired();
    }
}