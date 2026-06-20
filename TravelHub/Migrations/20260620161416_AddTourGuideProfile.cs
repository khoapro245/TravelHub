using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Migrations
{
    /// <inheritdoc />
    public partial class AddTourGuideProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Customer");

            migrationBuilder.AddColumn<int>(
                name: "ProviderID",
                table: "Tours",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TourGuideProfiles",
                columns: table => new
                {
                    ProfileID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Languages = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Locations = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Bio = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TourCategories = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdFrontUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IdBackUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CertUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    GuideAvatarUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsVerified = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TourGuideProfiles", x => x.ProfileID);
                    table.ForeignKey(
                        name: "FK_TourGuideProfiles_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tours_ProviderID",
                table: "Tours",
                column: "ProviderID");

            migrationBuilder.CreateIndex(
                name: "IX_TourGuideProfiles_UserID",
                table: "TourGuideProfiles",
                column: "UserID",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_Users_ProviderID",
                table: "Tours",
                column: "ProviderID",
                principalTable: "Users",
                principalColumn: "UserID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_Users_ProviderID",
                table: "Tours");

            migrationBuilder.DropTable(
                name: "TourGuideProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Tours_ProviderID",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProviderID",
                table: "Tours");
        }
    }
}
