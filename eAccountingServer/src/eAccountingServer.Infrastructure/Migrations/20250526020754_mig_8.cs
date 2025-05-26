using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyUsers_Users_UserId",
                table: "CompanyUsers");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "CompanyUsers",
                newName: "AppUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyUsers_Users_AppUserId",
                table: "CompanyUsers",
                column: "AppUserId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CompanyUsers_Users_AppUserId",
                table: "CompanyUsers");

            migrationBuilder.RenameColumn(
                name: "AppUserId",
                table: "CompanyUsers",
                newName: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CompanyUsers_Users_UserId",
                table: "CompanyUsers",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
