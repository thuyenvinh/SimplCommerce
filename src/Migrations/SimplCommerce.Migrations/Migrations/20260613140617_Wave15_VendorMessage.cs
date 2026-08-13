using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave15_VendorMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendors_VendorMessage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerUserId = table.Column<long>(type: "bigint", nullable: false),
                    FromUserId = table.Column<long>(type: "bigint", nullable: false),
                    SenderKind = table.Column<int>(type: "int", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: true),
                    Body = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReadAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors_VendorMessage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorMessage_Core_User_CustomerUserId",
                        column: x => x.CustomerUserId,
                        principalTable: "Core_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorMessage_Core_User_FromUserId",
                        column: x => x.FromUserId,
                        principalTable: "Core_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorMessage_Core_Vendor_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Core_Vendor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorMessage_CustomerUserId",
                table: "Vendors_VendorMessage",
                column: "CustomerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorMessage_FromUserId",
                table: "Vendors_VendorMessage",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorMessage_VendorId",
                table: "Vendors_VendorMessage",
                column: "VendorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vendors_VendorMessage");
        }
    }
}
