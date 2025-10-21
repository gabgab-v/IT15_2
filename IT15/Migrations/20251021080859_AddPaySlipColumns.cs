using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddPaySlipColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyRate",
                table: "PaySlips",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "PaySlips",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours",
                table: "PaySlips",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OvertimePenaltyHours",
                table: "PaySlips",
                type: "decimal(18, 2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "WorkingDaysInMonth",
                table: "PaySlips",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyRate",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "OvertimePenaltyHours",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "WorkingDaysInMonth",
                table: "PaySlips");
        }
    }
}

