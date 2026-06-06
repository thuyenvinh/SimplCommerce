using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave5_CheckoutItem_LockedPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LockedPrice",
                table: "Checkouts_CheckoutItem",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedPriceOn",
                table: "Checkouts_CheckoutItem",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LockedPrice",
                table: "Checkouts_CheckoutItem");

            migrationBuilder.DropColumn(
                name: "LockedPriceOn",
                table: "Checkouts_CheckoutItem");
        }
    }
}
