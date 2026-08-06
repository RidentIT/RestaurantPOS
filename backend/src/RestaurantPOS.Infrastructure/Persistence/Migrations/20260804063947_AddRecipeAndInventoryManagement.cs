using System;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecipeAndInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntityType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PerformedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PerformedByName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Summary = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    DetailsJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoodsReceivedNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SupplierId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceivedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReceivedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsReceivedNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RawMaterials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    UnitOfMeasurement = table.Column<int>(type: "INTEGER", nullable: false),
                    MainStoreReorderLevel = table.Column<decimal>(type: "TEXT", nullable: true),
                    KitchenParLevel = table.Column<decimal>(type: "TEXT", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RawMaterials", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Recipes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Recipes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockLevels",
                columns: table => new
                {
                    RawMaterialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Store = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityOnHand = table.Column<decimal>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockLevels", x => new { x.RawMaterialId, x.Store });
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RawMaterialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Store = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PerformedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OccurredAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockReleases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockReleases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecipeLines",
                columns: table => new
                {
                    RecipeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RawMaterialId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Quantity = table.Column<decimal>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecipeLines", x => new { x.RecipeId, x.RawMaterialId });
                    table.ForeignKey(
                        name: "FK_RecipeLines_Recipes_RecipeId",
                        column: x => x.RecipeId,
                        principalTable: "Recipes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogEntries_EntityType_EntityId",
                table: "AuditLogEntries",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceivedNotes_ReceivedAtUtc",
                table: "GoodsReceivedNotes",
                column: "ReceivedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_Name",
                table: "MenuItems",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RawMaterials_Name",
                table: "RawMaterials",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Recipes_MenuItemId",
                table: "Recipes",
                column: "MenuItemId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ReferenceId",
                table: "StockMovements",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_Store_RawMaterialId_OccurredAtUtc",
                table: "StockMovements",
                columns: new[] { "Store", "RawMaterialId", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_StockReleases_RequestedAtUtc",
                table: "StockReleases",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_Name",
                table: "Suppliers",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogEntries");

            migrationBuilder.DropTable(
                name: "GoodsReceivedNotes");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "RawMaterials");

            migrationBuilder.DropTable(
                name: "RecipeLines");

            migrationBuilder.DropTable(
                name: "StockLevels");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "StockReleases");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Recipes");
        }
    }
}