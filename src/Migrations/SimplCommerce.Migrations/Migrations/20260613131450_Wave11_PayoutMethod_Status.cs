using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave11_PayoutMethod_Status : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedOn",
                table: "Vendors_VendorPayout",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Method",
                table: "Vendors_VendorPayout",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ProviderResponse",
                table: "Vendors_VendorPayout",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentOn",
                table: "Vendors_VendorPayout",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Vendors_VendorPayout",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MomoPartnerCode",
                table: "Core_Vendor",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "Core_Vendor",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VnpayMerchantId",
                table: "Core_Vendor",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedOn",
                table: "Vendors_VendorPayout");

            migrationBuilder.DropColumn(
                name: "Method",
                table: "Vendors_VendorPayout");

            migrationBuilder.DropColumn(
                name: "ProviderResponse",
                table: "Vendors_VendorPayout");

            migrationBuilder.DropColumn(
                name: "SentOn",
                table: "Vendors_VendorPayout");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Vendors_VendorPayout");

            migrationBuilder.DropColumn(
                name: "MomoPartnerCode",
                table: "Core_Vendor");

            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "Core_Vendor");

            migrationBuilder.DropColumn(
                name: "VnpayMerchantId",
                table: "Core_Vendor");
        }
    }
}
