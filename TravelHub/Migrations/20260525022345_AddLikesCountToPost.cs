using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Migrations
{
    /// <inheritdoc />
    public partial class AddLikesCountToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LikesCount",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LikesCount",
                table: "Posts");
        }
    }
}
