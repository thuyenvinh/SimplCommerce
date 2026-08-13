using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave14_VendorDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendors_VendorDocument",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VendorApplicationId = table.Column<long>(type: "bigint", nullable: true),
                    VendorId = table.Column<long>(type: "bigint", nullable: true),
                    MediaId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    VerifiedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AdminNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors_VendorDocument", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorDocument_Core_Media_MediaId",
                        column: x => x.MediaId,
                        principalTable: "Core_Media",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorDocument_Core_User_UploadedByUserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "Core_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorDocument_Core_User_VerifiedByUserId",
                        column: x => x.VerifiedByUserId,
                        principalTable: "Core_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorDocument_Core_Vendor_VendorId",
                        column: x => x.VendorId,
                        principalTable: "Core_Vendor",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorDocument_Vendors_VendorApplication_VendorApplicationId",
                        column: x => x.VendorApplicationId,
                        principalTable: "Vendors_VendorApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorDocument_MediaId",
                table: "Vendors_VendorDocument",
                column: "MediaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorDocument_UploadedByUserId",
                table: "Vendors_VendorDocument",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorDocument_VendorApplicationId",
                table: "Vendors_VendorDocument",
                column: "VendorApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorDocument_VendorId",
                table: "Vendors_VendorDocument",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorDocument_VerifiedByUserId",
                table: "Vendors_VendorDocument",
                column: "VerifiedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vendors_VendorDocument");
        }
    }
}
