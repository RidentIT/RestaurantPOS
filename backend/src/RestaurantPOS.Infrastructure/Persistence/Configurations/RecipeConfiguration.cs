using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        // At most one recipe per size (BR-REC-001).
        builder.HasIndex(r => r.MenuItemVariantId).IsUnique();

        builder.Property(r => r.IsEnabled).IsRequired();

        builder.Metadata
            .FindNavigation(nameof(Recipe.Lines))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Lines)
            .WithOne()
            .HasForeignKey(l => l.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class RecipeLineConfiguration : IEntityTypeConfiguration<RecipeLine>
{
    public void Configure(EntityTypeBuilder<RecipeLine> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RecipeLines");

        // A raw material cannot appear more than once in the same recipe.
        builder.HasKey(l => new { l.RecipeId, l.RawMaterialId });

        builder.Property(l => l.Quantity).IsRequired();
    }
}