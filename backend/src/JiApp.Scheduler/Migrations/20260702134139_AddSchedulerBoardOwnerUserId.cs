using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.Scheduler.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulerBoardOwnerUserId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OwnerUserId",
                table: "Boards",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                migrationBuilder.Sql(
                    "UPDATE Boards SET OwnerUserId = json_extract(MemberUserIds, '$[0]') " +
                    "WHERE MemberUserIds IS NOT NULL AND json_array_length(MemberUserIds) > 0;");
            }
            else // Npgsql
            {
                migrationBuilder.Sql(
                    "UPDATE \"Boards\" SET \"OwnerUserId\" = (\"MemberUserIds\"::jsonb->>0)::bigint " +
                    "WHERE \"MemberUserIds\" IS NOT NULL AND jsonb_array_length(\"MemberUserIds\"::jsonb) > 0;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "Boards");
        }
    }
}
