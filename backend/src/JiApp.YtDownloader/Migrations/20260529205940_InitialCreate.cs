using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.YtDownloader.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", maxLength: 50000, nullable: true),
                    Exception = table.Column<string>(type: "TEXT", maxLength: 20000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YoutubeDownloadHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    DownloadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    VideoTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    VideoDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    VideoId = table.Column<string>(type: "TEXT", maxLength: 150, nullable: true),
                    VideoUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    ImageUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoutubeDownloadHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "YoutubeSearchHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SearchText = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsArchived = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YoutubeSearchHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventLogs_UserId",
                table: "EventLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_YoutubeDownloadHistory_UserId",
                table: "YoutubeDownloadHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_YoutubeSearchHistory_UserId",
                table: "YoutubeSearchHistory",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLogs");

            migrationBuilder.DropTable(
                name: "YoutubeDownloadHistory");

            migrationBuilder.DropTable(
                name: "YoutubeSearchHistory");
        }
    }
}
