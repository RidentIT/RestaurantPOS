using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class StewardConfiguration : IEntityTypeConfiguration<Steward>
{
    public void Configure(EntityTypeBuilder<Steward> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Stewards");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(Steward.NameMaxLength);

        builder.Property(s => s.IsActive).IsRequired();

        // Two stewards with the same name would make the picker and the sales report ambiguous.
        builder.HasIndex(s => s.Name).IsUnique();
    }
}
