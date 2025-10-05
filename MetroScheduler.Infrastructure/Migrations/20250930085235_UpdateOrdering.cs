using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetroStations_MetroLineId",
                table: "MetroStations");

            migrationBuilder.AddColumn<int>(
                name: "OrderIndex",
                table: "MetroStations",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MetroStations_MetroLineId_OrderIndex",
                table: "MetroStations",
                columns: new[] { "MetroLineId", "OrderIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MetroStations_MetroLineId_OrderIndex",
                table: "MetroStations");

            migrationBuilder.DropColumn(
                name: "OrderIndex",
                table: "MetroStations");

            migrationBuilder.CreateIndex(
                name: "IX_MetroStations_MetroLineId",
                table: "MetroStations",
                column: "MetroLineId");
        }
    }
}
