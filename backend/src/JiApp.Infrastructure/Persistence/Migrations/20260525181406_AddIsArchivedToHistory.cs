using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsArchivedToHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "YoutubeSearchHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsArchived",
                table: "YoutubeDownloadHistory",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "YoutubeSearchHistory");

            migrationBuilder.DropColumn(
                name: "IsArchived",
                table: "YoutubeDownloadHistory");
        }
    }
}
