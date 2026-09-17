using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations.CompanyDb
{
    /// <inheritdoc />
    public partial class mig_product_erp_mapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ErpProductId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ErpProductId",
                table: "Products");
        }
    }
}
