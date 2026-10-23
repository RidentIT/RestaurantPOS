using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierShifts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShiftId",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashierShifts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CashierUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CheckedInAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "TEXT", nullable: false),
                    CheckedOutAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ClosingCash = table.Column<decimal>(type: "TEXT", nullable: true),
                    ExpectedCash = table.Column<decimal>(type: "TEXT", nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ClosedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashierShifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashierShifts_Users_CashierUserId",
                        column: x => x.CashierUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShiftId",
                table: "Orders",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_CashierUserId_CheckedInAtUtc",
                table: "CashierShifts",
                columns: new[] { "CashierUserId", "CheckedInAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CashierShifts_OneOpenPerCashier",
                table: "CashierShifts",
                column: "CashierUserId",
                unique: true,
                filter: "\"CheckedOutAtUtc\" IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_CashierShifts_ShiftId",
                table: "Orders",
                column: "ShiftId",
                principalTable: "CashierShifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_CashierShifts_ShiftId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "CashierShifts");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShiftId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "Orders");
        }
    }
}
