using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave2_PaymentRefund_ShipmentStatus_CheckoutIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Default to Pending (1) so pre-migration shipments backfill to a valid
            // enum value rather than the EF-scaffolded 0 (which is no enum member).
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Shipments_Shipment",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<decimal>(
                name: "RefundedAmount",
                table: "Payments_Payment",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "RefundedOn",
                table: "Payments_Payment",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OrderCreatedId",
                table: "Checkouts_Checkout",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Shipments_Shipment");

            migrationBuilder.DropColumn(
                name: "RefundedAmount",
                table: "Payments_Payment");

            migrationBuilder.DropColumn(
                name: "RefundedOn",
                table: "Payments_Payment");

            migrationBuilder.DropColumn(
                name: "OrderCreatedId",
                table: "Checkouts_Checkout");
        }
    }
}
