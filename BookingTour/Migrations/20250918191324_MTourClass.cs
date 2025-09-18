using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingTour.Migrations
{
    /// <inheritdoc />
    public partial class MTourClass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDelete",
                table: "Tours",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "IsDelete",
                table: "Tours");
        }
    }
}
