using IT15.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260101000000_AddJobApplicationEmailConfirmation")]
    public partial class AddJobApplicationEmailConfirmation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "JobApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EmailConfirmationToken",
                table: "JobApplications",
                type: "text",
                nullable: true);

            // Mark already-approved applications as confirmed to avoid blocking existing users.
            migrationBuilder.Sql("UPDATE \"JobApplications\" SET \"EmailConfirmed\" = TRUE WHERE \"Status\" = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "EmailConfirmationToken",
                table: "JobApplications");
        }
    }
}
