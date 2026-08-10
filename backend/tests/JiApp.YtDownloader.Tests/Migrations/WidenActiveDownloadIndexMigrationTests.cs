using JiApp.YtDownloader.Domain;
using JiApp.YtDownloader.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Migrations;

/// <summary>
/// Guards the deploy-safety normalization in
/// <see cref="JiApp.YtDownloader.Migrations.WidenActiveDownloadIndex"/>: the widened unique
/// index can only be created if pre-existing colliding rows are demoted first. This test
/// seeds the bug's residue and proves the migration survives it.
/// </summary>
public sealed class WidenActiveDownloadIndexMigrationTests
{
    private const string OldMigrationId = "20260805120000_AddDownloadCommand";
    private const string IndexName = "IX_DownloadCommands_UserId_VideoId";

    [Fact]
    public void Migrate_WidensIndex_AndDemotesCollidingFailedRows()
    {
        using var fixture = new Fixture();

        // Bug residue: a live Queued row alongside a Failed row awaiting retry.
        fixture.InsertRow("live-queued", 1, "video-a", "Queued", nextAttemptAt: null, createdAt: T(0));
        fixture.InsertRow("failed-loser", 1, "video-a", "Failed", nextAttemptAt: T(1), createdAt: T(0));

        // Bug residue: two Failed rows awaiting retry for the same video.
        fixture.InsertRow("failed-older", 2, "video-b", "Failed", nextAttemptAt: T(1), createdAt: T(0));
        fixture.InsertRow("failed-newer", 2, "video-b", "Failed", nextAttemptAt: T(2), createdAt: T(1));

        // Control: a lone Failed row awaiting retry is a legitimate scheduled retry.
        fixture.InsertRow("failed-alone", 3, "video-c", "Failed", nextAttemptAt: T(1), createdAt: T(0));

        var totalBefore = fixture.CountAll();

        var act = () => fixture.MigrateToLatest();

        act.Should().NotThrow();
        fixture.IndexExists(IndexName).Should().BeTrue();
        fixture.CountAll().Should().Be(totalBefore);

        // Live row untouched; its competing Failed retry is demoted to the dead-letter state.
        fixture.Status("live-queued").Should().Be(DownloadCommandStatus.Queued);
        fixture.NextAttemptAt("failed-loser").Should().BeNull();

        // Two Failed retries: exactly one keeps its retry — the newer one survives.
        fixture.NextAttemptAt("failed-older").Should().BeNull();
        fixture.NextAttemptAt("failed-newer").Should().NotBeNull();

        // A lone Failed retry is untouched.
        fixture.NextAttemptAt("failed-alone").Should().NotBeNull();
    }

    private static DateTime T(int second) => new(2030, 1, 1, 0, 0, second, DateTimeKind.Utc);

    private sealed class Fixture : IDisposable
    {
        // Fixed-width second-precision text so lexicographic ordering matches chronological
        // ordering in the migration's normalization SQL (CreatedAtUtc comparisons).
        private const string Fmt = "yyyy-MM-dd HH:mm:ss";

        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<YtDbContext> _options;

        public Fixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<YtDbContext>()
                .UseSqlite(_connection)
                .Options;

            // Apply everything up to (but not including) the migration under test.
            using (var db = new YtDbContext(_options))
                db.GetInfrastructure().GetRequiredService<IMigrator>().Migrate(OldMigrationId);
        }

        public void InsertRow(string id, long userId, string videoId, string status, DateTime? nextAttemptAt, DateTime createdAt)
        {
            using var db = new YtDbContext(_options);
            db.Database.ExecuteSqlRaw(
                """
                INSERT INTO "DownloadCommands"
                    ("Id", "UserId", "VideoId", "VideoTitle", "VideoDescription", "VideoImageUrl", "VideoUrl",
                     "Status", "AttemptsRemaining", "LastError", "ErrorCategory", "NextAttemptAt", "FilePath",
                     "ExpiresAt", "CreatedAtUtc")
                VALUES ({0}, {1}, {2}, 'Title', NULL, NULL, 'https://youtube.com/watch?v=test',
                        {3}, 3, 'transient error', 'youtube-dl', {4}, NULL,
                        {5}, {6})
                """,
                id, userId, videoId, status,
                nextAttemptAt?.ToString(Fmt),
                createdAt.AddMinutes(15).ToString(Fmt),
                createdAt.ToString(Fmt));
        }

        public void MigrateToLatest()
        {
            using var db = new YtDbContext(_options);
            db.Database.Migrate();
        }

        public int CountAll()
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.Count();
        }

        public DownloadCommandStatus? Status(string id) => Load(id)?.Status;

        public DateTime? NextAttemptAt(string id) => Load(id)?.NextAttemptAt;

        public bool IndexExists(string indexName)
        {
            using var db = new YtDbContext(_options);
            return db.Database.SqlQueryRaw<string>(
                "SELECT name FROM sqlite_master WHERE type = 'index' AND name = {0}", indexName).Any();
        }

        private DownloadCommand? Load(string id)
        {
            using var db = new YtDbContext(_options);
            return db.DownloadCommands.AsNoTracking().FirstOrDefault(c => c.Id == id);
        }

        public void Dispose() => _connection.Dispose();
    }
}
