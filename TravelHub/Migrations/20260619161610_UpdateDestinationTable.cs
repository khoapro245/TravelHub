using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDestinationTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OpenWeatherMapCityID",
                table: "Destinations",
                newName: "KeyMain");

            migrationBuilder.RenameColumn(
                name: "EstimatedBaseCostVND",
                table: "Destinations",
                newName: "TourPricePerPerson");

            migrationBuilder.AddColumn<decimal>(
                name: "AccommodationCost",
                table: "Destinations",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "EntranceFee",
                table: "Destinations",
                type: "decimal(18,0)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Image",
                table: "Destinations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Rate",
                table: "Destinations",
                type: "decimal(18,1)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTourCost",
                table: "Destinations",
                type: "decimal(18,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccommodationCost",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "EntranceFee",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "Image",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "Rate",
                table: "Destinations");

            migrationBuilder.DropColumn(
                name: "TotalTourCost",
                table: "Destinations");

            migrationBuilder.RenameColumn(
                name: "TourPricePerPerson",
                table: "Destinations",
                newName: "EstimatedBaseCostVND");

            migrationBuilder.RenameColumn(
                name: "KeyMain",
                table: "Destinations",
                newName: "OpenWeatherMapCityID");
        }
    }
}
