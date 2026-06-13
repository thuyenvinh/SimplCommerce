using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SimplCommerce.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class Wave7_VendorApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Vendors_VendorApplication",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicantUserId = table.Column<long>(type: "bigint", nullable: false),
                    BusinessName = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ContactPhone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RegistrationNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    DecidedByUserId = table.Column<long>(type: "bigint", nullable: true),
                    DecidedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedVendorId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LatestUpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors_VendorApplication", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_VendorApplication_Core_User_ApplicantUserId",
                        column: x => x.ApplicantUserId,
                        principalTable: "Core_User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_VendorApplication_ApplicantUserId",
                table: "Vendors_VendorApplication",
                column: "ApplicantUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Vendors_VendorApplication");
        }
    }
}
