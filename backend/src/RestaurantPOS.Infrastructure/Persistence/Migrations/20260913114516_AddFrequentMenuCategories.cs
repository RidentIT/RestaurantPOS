using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFrequentMenuCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrequentMenuCategories",
                table: "RestaurantSettings",
                type: "TEXT",
                maxLength: 2000,
                nullable: true);

            // Backfills the single existing settings row with the same defaults a brand-new
            // restaurant gets from RestaurantSettings.Create, joined with the entity's own
            // separator (U+001F), so a restaurant already running today doesn't lose the pinned
            // categories it's used to seeing just because this column didn't exist until now.
            migrationBuilder.Sql(
                $"UPDATE \"RestaurantSettings\" SET \"FrequentMenuCategories\" = " +
                $"'KottuRice and CurryCheese KottuString Hoppers' " +
                "WHERE \"FrequentMenuCategories\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrequentMenuCategories",
                table: "RestaurantSettings");
        }
    }
}
