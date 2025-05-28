using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations.CompanyDb
{
    /// <inheritdoc />
    public partial class mig_4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterDetails_CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails",
                column: "CashRegisterDetailOpasiteId");

            migrationBuilder.CreateIndex(
                name: "IX_CashRegisterDetails_CashRegisterId",
                table: "CashRegisterDetails",
                column: "CashRegisterId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisterDetails_CashRegisterDetails_CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails",
                column: "CashRegisterDetailOpasiteId",
                principalTable: "CashRegisterDetails",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CashRegisterDetails_CashRegisters_CashRegisterId",
                table: "CashRegisterDetails",
                column: "CashRegisterId",
                principalTable: "CashRegisters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisterDetails_CashRegisterDetails_CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_CashRegisterDetails_CashRegisters_CashRegisterId",
                table: "CashRegisterDetails");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisterDetails_CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails");

            migrationBuilder.DropIndex(
                name: "IX_CashRegisterDetails_CashRegisterId",
                table: "CashRegisterDetails");

            migrationBuilder.DropColumn(
                name: "CashRegisterDetailOpasiteId",
                table: "CashRegisterDetails");
        }
    }
}
