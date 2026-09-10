using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StewardId",
                table: "Orders",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Stewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stewards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StewardId",
                table: "Orders",
                column: "StewardId");

            migrationBuilder.CreateIndex(
                name: "IX_Stewards_Name",
                table: "Stewards",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Stewards_StewardId",
                table: "Orders",
                column: "StewardId",
                principalTable: "Stewards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Stewards_StewardId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "Stewards");

            migrationBuilder.DropIndex(
                name: "IX_Orders_StewardId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "StewardId",
                table: "Orders");
        }
    }
}
