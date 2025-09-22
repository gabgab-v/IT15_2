using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOvertimePenaltyToPaySlip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OvertimePenalty",
                table: "PaySlips",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OvertimePenalty",
                table: "PaySlips");
        }
    }
}
