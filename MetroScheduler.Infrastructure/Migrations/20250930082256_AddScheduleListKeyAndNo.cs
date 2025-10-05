using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleListKeyAndNo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListNo",
                table: "StationSchedules",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ListNo",
                table: "StationSchedules");
        }
    }
}
