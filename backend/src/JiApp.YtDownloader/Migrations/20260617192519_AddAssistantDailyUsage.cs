using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.YtDownloader.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantDailyUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantDailyUsage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    UsageDateUtc = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantDailyUsage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDailyUsage_UserId",
                table: "AssistantDailyUsage",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantDailyUsage_UserId_UsageDateUtc",
                table: "AssistantDailyUsage",
                columns: new[] { "UserId", "UsageDateUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantDailyUsage");
        }
    }
}
