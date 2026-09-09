using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RestaurantPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRestaurantSettingsAndBillCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ServiceChargeRatePercent",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxRatePercent",
                table: "Orders",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "RestaurantSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    AddressLine1 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    LogoPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    TaxRatePercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    ServiceChargeRatePercent = table.Column<decimal>(type: "TEXT", nullable: false),
                    ReceiptFooterMessage = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DefaultPrinterName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ApprovalPinMaxAttempts = table.Column<int>(type: "INTEGER", nullable: false),
                    ApprovalPinLockoutMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    BackupFolderPath = table.Column<string>(type: "TEXT", maxLength: 400, nullable: true),
                    BackupRetentionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RestaurantSettings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RestaurantSettings");

            migrationBuilder.DropColumn(
                name: "ServiceChargeRatePercent",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "TaxRatePercent",
                table: "Orders");
        }
    }
}
