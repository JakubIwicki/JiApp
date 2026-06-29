using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.JiApp.LovingBoards.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BoardItems",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BoardId = table.Column<long>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Quantity = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Note = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    AssigneeUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsRecurring = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AddedByUserId = table.Column<long>(type: "INTEGER", nullable: false),
                    CompletedByUserId = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardItems_BoardId",
                table: "BoardItems",
                column: "BoardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoardItems");
        }
    }
}
