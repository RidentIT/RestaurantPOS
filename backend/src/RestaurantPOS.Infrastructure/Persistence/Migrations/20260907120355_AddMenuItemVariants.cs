using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemVariants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_MenuItems_MenuItemId",
                table: "OrderItems");

            // The new table is created (and seeded) before MenuItems.Price disappears and before
            // Recipes/OrderItems are renamed below. That ordering is deliberate: the one synthetic
            // variant created for each existing menu item reuses that item's own id, so every FK
            // value already sitting in Recipes.MenuItemId / OrderItems.MenuItemId — still holding
            // the old MenuItem.Id at this point — is already correct once those columns are simply
            // renamed further down. No separate id-rewriting pass is needed, since the ids never
            // actually change, only what they point at.
            migrationBuilder.CreateTable(
                name: "MenuItemVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    MenuItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 40, nullable: true),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItemVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItemVariants_MenuItems_MenuItemId",
                        column: x => x.MenuItemId,
                        principalTable: "MenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MenuItemVariants_MenuItemId",
                table: "MenuItemVariants",
                column: "MenuItemId");

            // One unnamed size per existing item, priced at what the item used to cost and stamped
            // with the item's own timestamps, reusing the item's own id (see the comment above).
            migrationBuilder.Sql(
                """
                INSERT INTO MenuItemVariants (Id, MenuItemId, Name, Price, SortOrder, CreatedAtUtc, UpdatedAtUtc)
                SELECT Id, Id, NULL, Price, 0, CreatedAtUtc, UpdatedAtUtc FROM MenuItems;
                """);

            migrationBuilder.DropColumn(
                name: "Price",
                table: "MenuItems");

            migrationBuilder.RenameColumn(
                name: "MenuItemId",
                table: "Recipes",
                newName: "MenuItemVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_MenuItemId",
                table: "Recipes",
                newName: "IX_Recipes_MenuItemVariantId");

            migrationBuilder.RenameColumn(
                name: "MenuItemId",
                table: "OrderItems",
                newName: "MenuItemVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_MenuItemId",
                table: "OrderItems",
                newName: "IX_OrderItems_MenuItemVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_MenuItemVariants_MenuItemVariantId",
                table: "OrderItems",
                column: "MenuItemVariantId",
                principalTable: "MenuItemVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MenuItemVariantId",
                table: "Recipes",
                newName: "MenuItemId");

            migrationBuilder.RenameIndex(
                name: "IX_Recipes_MenuItemVariantId",
                table: "Recipes",
                newName: "IX_Recipes_MenuItemId");

            migrationBuilder.RenameColumn(
                name: "MenuItemVariantId",
                table: "OrderItems",
                newName: "MenuItemId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_MenuItemVariantId",
                table: "OrderItems",
                newName: "IX_OrderItems_MenuItemId");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "MenuItems",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // Reverses the Up backfill while MenuItemVariants still exists to read from. A menu
            // item split into several sizes since this migration ran has no single price to go
            // back to — it simply keeps the column's default.
            migrationBuilder.Sql(
                """
                UPDATE MenuItems
                SET Price = (SELECT Price FROM MenuItemVariants WHERE MenuItemVariants.MenuItemId = MenuItems.Id LIMIT 1)
                WHERE EXISTS (SELECT 1 FROM MenuItemVariants WHERE MenuItemVariants.MenuItemId = MenuItems.Id);
                """);

            // SQLite has no ALTER TABLE ... DROP CONSTRAINT: OrderItems' old FK to MenuItemVariants
            // survived the column rename above under its new name, still pointing at that table, so
            // it can only be removed by rebuilding OrderItems. That has to happen — by hand, since
            // letting DropForeignKey/AddForeignKey do it defers the rebuild past our DropTable below
            // regardless of where those calls sit in this method — before MenuItemVariants is
            // dropped, or a connection with foreign-key enforcement on (Microsoft.Data.Sqlite's
            // default, unlike the sqlite3 CLI's) refuses the drop with "FOREIGN KEY constraint
            // failed". The PRAGMA toggle runs outside a transaction, as SQLite requires for it to
            // take effect at all.
            migrationBuilder.Sql(
                """
                CREATE TABLE "ef_temp_OrderItems" (
                    "Id" TEXT NOT NULL CONSTRAINT "PK_OrderItems" PRIMARY KEY,
                    "CancelledAtUtc" TEXT NULL,
                    "CreatedAtUtc" TEXT NOT NULL,
                    "IsCancelled" INTEGER NOT NULL,
                    "MenuItemId" TEXT NOT NULL,
                    "MenuItemName" TEXT NOT NULL,
                    "OrderId" TEXT NOT NULL,
                    "Quantity" INTEGER NOT NULL,
                    "SpecialInstructions" TEXT NULL,
                    "UnitPrice" TEXT NOT NULL,
                    "UpdatedAtUtc" TEXT NULL,
                    CONSTRAINT "FK_OrderItems_MenuItems_MenuItemId" FOREIGN KEY ("MenuItemId") REFERENCES "MenuItems" ("Id") ON DELETE RESTRICT,
                    CONSTRAINT "FK_OrderItems_Orders_OrderId" FOREIGN KEY ("OrderId") REFERENCES "Orders" ("Id") ON DELETE CASCADE
                );

                INSERT INTO "ef_temp_OrderItems" ("Id", "CancelledAtUtc", "CreatedAtUtc", "IsCancelled", "MenuItemId", "MenuItemName", "OrderId", "Quantity", "SpecialInstructions", "UnitPrice", "UpdatedAtUtc")
                SELECT "Id", "CancelledAtUtc", "CreatedAtUtc", "IsCancelled", "MenuItemId", "MenuItemName", "OrderId", "Quantity", "SpecialInstructions", "UnitPrice", "UpdatedAtUtc"
                FROM "OrderItems";
                """);

            migrationBuilder.Sql("PRAGMA foreign_keys = 0;", suppressTransaction: true);

            migrationBuilder.Sql(
                """
                DROP TABLE "OrderItems";
                ALTER TABLE "ef_temp_OrderItems" RENAME TO "OrderItems";
                """);

            migrationBuilder.Sql("PRAGMA foreign_keys = 1;", suppressTransaction: true);

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_OrderItems_MenuItemId" ON "OrderItems" ("MenuItemId");
                CREATE INDEX "IX_OrderItems_OrderId" ON "OrderItems" ("OrderId");
                """);

            migrationBuilder.DropTable(
                name: "MenuItemVariants");
        }
    }
}
