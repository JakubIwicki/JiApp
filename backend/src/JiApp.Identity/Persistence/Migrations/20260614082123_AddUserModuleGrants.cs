using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserModuleGrants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserModuleGrants",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<long>(type: "INTEGER", nullable: false),
                    ModuleName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModuleGrants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserModuleGrants_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserModuleGrants_UserId_ModuleName",
                table: "UserModuleGrants",
                columns: new[] { "UserId", "ModuleName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserModuleGrants");
        }
    }
}
