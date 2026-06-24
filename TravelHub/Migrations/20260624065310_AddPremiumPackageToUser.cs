using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TravelHub.Migrations
{
    /// <inheritdoc />
    public partial class AddPremiumPackageToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPremium",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "PremiumExpiryDate",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsPremium",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PremiumExpiryDate",
                table: "Users");
        }
    }
}
