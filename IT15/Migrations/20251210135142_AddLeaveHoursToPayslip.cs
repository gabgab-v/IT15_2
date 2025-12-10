using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaveHoursToPayslip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AbsentHours",
                table: "PaySlips",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LeaveHours",
                table: "PaySlips",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbsentHours",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "LeaveHours",
                table: "PaySlips");
        }
    }
}
