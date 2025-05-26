using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_7 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyUsers_Users_AppUserId",
                table: "CompanyUsers");

            migrationBuilder.DropIndex(
                name: "IX_CompanyUsers_AppUserId",
                table: "CompanyUsers");

            migrationBuilder.DropColumn(
                name: "AppUserId",
                table: "CompanyUsers");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyUsers_Users_UserId",
                table: "CompanyUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyUsers_Users_UserId",
                table: "CompanyUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "AppUserId",
                table: "CompanyUsers",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompanyUsers_AppUserId",
                table: "CompanyUsers",
                column: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyUsers_Users_AppUserId",
                table: "CompanyUsers",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }
    }
}
