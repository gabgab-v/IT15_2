using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovedById",
                table: "Payrolls",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateApproved",
                table: "Payrolls",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Payrolls",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ApprovedById",
                table: "Payrolls",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_AspNetUsers_ApprovedById",
                table: "Payrolls",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_AspNetUsers_ApprovedById",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_ApprovedById",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "DateApproved",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Payrolls");
        }
    }
}
