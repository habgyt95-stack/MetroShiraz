using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MetroScheduler.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMetroFullSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MetroFullSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainNumber = table.Column<long>(type: "bigint", nullable: false),
                    ListNo = table.Column<int>(type: "int", nullable: false),
                    EndStation = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TimeOrigin = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    TimeDestination = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsHoliday = table.Column<bool>(type: "bit", nullable: false),
                    LineId = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    LineName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StationIdOrigin = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StationIdDestination = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StationNameOrigin = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    StationNameDestination = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SchedulesNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    LastUpdated = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MetroFullSchedules", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MetroFullSchedules");
        }
    }
}
