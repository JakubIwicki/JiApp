using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace api.JiApp.LovingBoards.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardItemCascadeForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_BoardItems_Boards_BoardId",
                table: "BoardItems",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardItems_Boards_BoardId",
                table: "BoardItems");
        }
    }
}
