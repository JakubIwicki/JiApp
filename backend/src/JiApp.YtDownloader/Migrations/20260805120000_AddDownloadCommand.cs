using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.YtDownloader.Migrations
{
    /// <inheritdoc />
    public partial class AddDownloadCommand : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DownloadCommands",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    VideoId = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    VideoTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    VideoDescription = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    VideoImageUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    VideoUrl = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AttemptsRemaining = table.Column<int>(type: "INTEGER", nullable: false),
                    LastError = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ErrorCategory = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    NextAttemptAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 1024, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DownloadCommands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DownloadCommands_UserId_VideoId",
                table: "DownloadCommands",
                columns: new[] { "UserId", "VideoId" },
                unique: true,
                filter: "\"Status\" IN ('Queued', 'Processing')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DownloadCommands");
        }
    }
}
