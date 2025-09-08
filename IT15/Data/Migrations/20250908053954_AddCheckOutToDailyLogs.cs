using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckOutToDailyLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "DailyLogs",
                newName: "CheckInTime");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckOutTime",
                table: "DailyLogs",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckOutTime",
                table: "DailyLogs");

            migrationBuilder.RenameColumn(
                name: "CheckInTime",
                table: "DailyLogs",
                newName: "Date");
        }
    }
}
