using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IT15.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAndCostToSupplies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryServiceId",
                table: "SupplyRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalCost",
                table: "SupplyRequests",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Cost",
                table: "Supplies",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "DeliveryServices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Fee = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryServices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SupplyRequests_DeliveryServiceId",
                table: "SupplyRequests",
                column: "DeliveryServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_SupplyRequests_DeliveryServices_DeliveryServiceId",
                table: "SupplyRequests",
                column: "DeliveryServiceId",
                principalTable: "DeliveryServices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SupplyRequests_DeliveryServices_DeliveryServiceId",
                table: "SupplyRequests");

            migrationBuilder.DropTable(
                name: "DeliveryServices");

            migrationBuilder.DropIndex(
                name: "IX_SupplyRequests_DeliveryServiceId",
                table: "SupplyRequests");

            migrationBuilder.DropColumn(
                name: "DeliveryServiceId",
                table: "SupplyRequests");

            migrationBuilder.DropColumn(
                name: "TotalCost",
                table: "SupplyRequests");

            migrationBuilder.DropColumn(
                name: "Cost",
                table: "Supplies");
        }
    }
}
