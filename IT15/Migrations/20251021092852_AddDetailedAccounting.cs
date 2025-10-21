using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace IT15.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailedAccounting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CompanyLedger",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AccountsPayableId",
                table: "CompanyLedger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AccountsReceivableId",
                table: "CompanyLedger",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "CompanyLedger",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Counterparty",
                table: "CompanyLedger",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CompanyLedger",
                type: "timestamptz",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "EntryType",
                table: "CompanyLedger",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNumber",
                table: "CompanyLedger",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "CompanyLedger",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ""CompanyLedger""
                SET ""EntryType"" = CASE WHEN ""Amount"" >= 0 THEN 0 ELSE 1 END,
                    ""Category"" = CASE
                        WHEN ""Description"" ILIKE 'Payroll%' THEN 1
                        WHEN ""Description"" ILIKE 'Operational Cost%' THEN 3
                        WHEN ""Description"" ILIKE 'Delivery Fee%' THEN 3
                        WHEN ""Description"" ILIKE 'Supply Cost%' THEN 2
                        WHEN ""Description"" ILIKE 'Sale%' THEN 0
                        ELSE 4
                    END,
                    ""ReferenceNumber"" = COALESCE(""ReferenceNumber"", 'LEG-' || LPAD(""Id""::text, 6, '0')),
                    ""Counterparty"" = COALESCE(""Counterparty"", 'Legacy import'),
                    ""CreatedAt"" = CASE
                        WHEN ""CreatedAt"" <= '0001-02-01'::timestamptz THEN COALESCE(""TransactionDate"", NOW())
                        ELSE ""CreatedAt""
                    END;
            ");

            migrationBuilder.CreateTable(
                name: "AccountsPayables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BillDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    BillAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpenseCategory = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsPayables", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountsReceivables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InvoiceDate = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    InvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RevenueCategory = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountsReceivables", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedger_AccountsPayableId",
                table: "CompanyLedger",
                column: "AccountsPayableId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyLedger_AccountsReceivableId",
                table: "CompanyLedger",
                column: "AccountsReceivableId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyLedger_AccountsPayables_AccountsPayableId",
                table: "CompanyLedger",
                column: "AccountsPayableId",
                principalTable: "AccountsPayables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyLedger_AccountsReceivables_AccountsReceivableId",
                table: "CompanyLedger",
                column: "AccountsReceivableId",
                principalTable: "AccountsReceivables",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyLedger_AccountsPayables_AccountsPayableId",
                table: "CompanyLedger");

            migrationBuilder.DropForeignKey(
                name: "FK_CompanyLedger_AccountsReceivables_AccountsReceivableId",
                table: "CompanyLedger");

            migrationBuilder.DropTable(
                name: "AccountsPayables");

            migrationBuilder.DropTable(
                name: "AccountsReceivables");

            migrationBuilder.DropIndex(
                name: "IX_CompanyLedger_AccountsPayableId",
                table: "CompanyLedger");

            migrationBuilder.DropIndex(
                name: "IX_CompanyLedger_AccountsReceivableId",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "AccountsPayableId",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "AccountsReceivableId",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "Counterparty",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "EntryType",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "ReferenceNumber",
                table: "CompanyLedger");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "CompanyLedger");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "CompanyLedger",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(512)",
                oldMaxLength: 512);
        }
    }
}
