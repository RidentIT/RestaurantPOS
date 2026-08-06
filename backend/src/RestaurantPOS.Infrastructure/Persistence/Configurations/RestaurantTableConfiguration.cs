using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using RestaurantPOS.Domain.Entities;

namespace RestaurantPOS.Infrastructure.Persistence.Configurations;

internal sealed class RestaurantTableConfiguration : IEntityTypeConfiguration<RestaurantTable>
{
    public void Configure(EntityTypeBuilder<RestaurantTable> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("RestaurantTables");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Number)
            .IsRequired()
            .HasMaxLength(RestaurantTable.NumberMaxLength);

        builder.Property(t => t.Notes).HasMaxLength(RestaurantTable.NotesMaxLength);

        // Two tables labelled "4" would make the floor plan ambiguous for staff and for search.
        builder.HasIndex(t => t.Number).IsUnique();
    }
}
