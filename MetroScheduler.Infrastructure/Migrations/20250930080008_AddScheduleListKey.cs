using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleListKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StationSchedules",
                table: "StationSchedules");

            migrationBuilder.AlterColumn<string>(
                name: "EndStation",
                table: "StationSchedules",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "StartStation",
                table: "StationSchedules",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AddColumn<string>(
                name: "ListKey",
                table: "StationSchedules",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_StationSchedules",
                table: "StationSchedules",
                columns: new[] { "StationId", "IsHoliday", "ListKey" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_StationSchedules",
                table: "StationSchedules");

            migrationBuilder.DropColumn(
                name: "ListKey",
                table: "StationSchedules");

            migrationBuilder.AlterColumn<string>(
                name: "StartStation",
                table: "StationSchedules",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EndStation",
                table: "StationSchedules",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_StationSchedules",
                table: "StationSchedules",
                columns: new[] { "StationId", "StartStation", "EndStation", "IsHoliday" });
        }
    }
}
