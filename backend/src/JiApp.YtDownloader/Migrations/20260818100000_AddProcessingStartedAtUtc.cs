using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.YtDownloader.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStartedAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAtUtc",
                table: "DownloadCommands",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingStartedAtUtc",
                table: "DownloadCommands");
        }
    }
}
