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

            // Backfill: existing boards get OwnerUserId = first element of MemberUserIds.
            // This is the creator because CreateBoardHandler initializes MemberUserIds = [creator]
            // and AddBoardMember only appends — index 0 is always the creator for app-created rows.
            if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                // Boards with an empty MemberUserIds array are intentionally left at OwnerUserId = 0:
                // such boards are already inaccessible (membership-gated Get/List can never return them),
                // so no owner is assigned.
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
