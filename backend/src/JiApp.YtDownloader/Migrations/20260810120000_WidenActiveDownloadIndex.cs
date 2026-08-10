using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JiApp.YtDownloader.Migrations
{
    /// <inheritdoc />
    public partial class WidenActiveDownloadIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Prod may already hold the residue of the fixed bug: for one (UserId, VideoId),
            // a Failed row awaiting retry alongside a Queued/Processing row, or two Failed
            // rows awaiting retry. The widened unique index below would reject both, so the
            // CREATE would throw and crash-loop the service on such a DB. Demote every losing
            // Failed row to the dead-letter state (NextAttemptAt = NULL) first: a live
            // Queued/Processing row always wins, and among Failed rows awaiting retry the
            // newest by (CreatedAtUtc, Id) survives. No rows are deleted.
            migrationBuilder.Sql(
                """
                UPDATE "DownloadCommands"
                SET "NextAttemptAt" = NULL
                WHERE "Status" = 'Failed' AND "NextAttemptAt" IS NOT NULL
                  AND EXISTS (
                      SELECT 1 FROM "DownloadCommands" AS "other"
                      WHERE "other"."UserId" = "DownloadCommands"."UserId"
                        AND "other"."VideoId" = "DownloadCommands"."VideoId"
                        AND "other"."Id" != "DownloadCommands"."Id"
                        AND (
                            "other"."Status" IN ('Queued', 'Processing')
                            OR (
                                "other"."Status" = 'Failed'
                                AND "other"."NextAttemptAt" IS NOT NULL
                                AND (
                                    "other"."CreatedAtUtc" > "DownloadCommands"."CreatedAtUtc"
                                    OR (
                                        "other"."CreatedAtUtc" = "DownloadCommands"."CreatedAtUtc"
                                        AND "other"."Id" > "DownloadCommands"."Id"
                                    )
                                )
                            )
                        )
                  );
                """);

            migrationBuilder.DropIndex(
                name: "IX_DownloadCommands_UserId_VideoId",
                table: "DownloadCommands");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadCommands_UserId_VideoId",
                table: "DownloadCommands",
                columns: new[] { "UserId", "VideoId" },
                unique: true,
                filter: "\"Status\" IN ('Queued', 'Processing') OR (\"Status\" = 'Failed' AND \"NextAttemptAt\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No normalization needed here — narrowing the filter back can never collide.
            migrationBuilder.DropIndex(
                name: "IX_DownloadCommands_UserId_VideoId",
                table: "DownloadCommands");

            migrationBuilder.CreateIndex(
                name: "IX_DownloadCommands_UserId_VideoId",
                table: "DownloadCommands",
                columns: new[] { "UserId", "VideoId" },
                unique: true,
                filter: "\"Status\" IN ('Queued', 'Processing')");
        }
    }
}
