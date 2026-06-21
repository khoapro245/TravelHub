using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminNoteToTourGuideProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminNote",
                table: "TourGuideProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminNote",
                table: "TourGuideProfiles");
        }
    }
}
