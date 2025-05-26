using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TaxDepartment",
                table: "Companies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Companies",
                type: "varchar(11)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TaxDepartment",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Companies");
        }
    }
}
