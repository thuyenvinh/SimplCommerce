using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave16_VendorShippingFlatFee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippingFlatFee",
                table: "Core_Vendor",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingFlatFee",
                table: "Core_Vendor");
        }
    }
}
