using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserToCompanyLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CompanyLedger",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedger_UserId",
                table: "CompanyLedger",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyLedger_AspNetUsers_UserId",
                table: "CompanyLedger",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyLedger_AspNetUsers_UserId",
                table: "CompanyLedger");

            migrationBuilder.DropIndex(
                name: "IX_CompanyLedger_UserId",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CompanyLedger");
        }
    }
}
