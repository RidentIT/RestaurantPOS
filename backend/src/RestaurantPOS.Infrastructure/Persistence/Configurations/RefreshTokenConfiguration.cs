using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        // The aggregate assigns the id, so the key is already populated by the time EF sees a
        // new token. Saying so explicitly stops EF from assuming a set key means an existing
        // row and issuing an UPDATE for a token that was only just created.
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        // Token lookup on refresh goes through this index.
        builder.HasIndex(t => t.TokenHash).IsUnique();

        builder.Property(t => t.ExpiresAtUtc).IsRequired();
        builder.Property(t => t.CreatedAtUtc).IsRequired();
    }
}