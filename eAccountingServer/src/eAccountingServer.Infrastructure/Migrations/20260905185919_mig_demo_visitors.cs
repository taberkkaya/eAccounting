using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eAccountingServer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class mig_demo_visitors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DemoVisitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    DisplayEmail = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CodeExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CodeAttempts = table.Column<int>(type: "int", nullable: false),
                    LastCodeSentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CodesSent = table.Column<int>(type: "int", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SessionCount = table.Column<int>(type: "int", nullable: false),
                    LastSessionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DemoVisitors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DemoVisitors_Email",
                table: "DemoVisitors",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DemoVisitors");
        }
    }
}
