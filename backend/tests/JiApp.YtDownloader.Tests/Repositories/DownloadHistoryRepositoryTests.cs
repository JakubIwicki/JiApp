using JiApp.Common.Models;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Tests.Repositories;

public sealed class DownloadHistoryRepositoryTests : HandlerTestBase<YtDbContext>
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;

    private static readonly DateTime BaseTime = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyOwnersRows_OrderedNewestFirst()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithEntry(UserId, "mine-older", BaseTime)
            .WithEntry(UserId, "mine-newer", BaseTime.AddHours(1))
            .WithEntry(OtherUserId, "theirs", BaseTime.AddHours(2));

        var results = await fixture.Sut.GetByUserIdAsync(UserId, 10);

        results.Select(h => h.VideoId).Should().Equal("mine-newer", "mine-older");
    }

    [Fact]
    public async Task GetByUserIdAsync_OmitsArchivedRows()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithEntry(UserId, "live", BaseTime)
            .WithArchivedEntry(UserId, "archived", BaseTime.AddHours(1));

        var results = await fixture.Sut.GetByUserIdAsync(UserId, 10);

        results.Select(h => h.VideoId).Should().Equal("live");
    }

    [Fact]
    public async Task GetByUserIdAsync_AppliesLimit()
    {
        var fixture = Fixture.Init(DbContext, Db);
        for (var i = 0; i < 20; i++)
            fixture.WithEntry(UserId, $"vid-{i}", BaseTime.AddMinutes(i));

        var results = await fixture.Sut.GetByUserIdAsync(UserId, 5);

        results.Should().HaveCount(5);
    }

    [Fact]
    public async Task GetByUserIdAsync_AppliesOffset_ForPaging()
    {
        var fixture = Fixture.Init(DbContext, Db);
        for (var i = 0; i < 10; i++)
            fixture.WithEntry(UserId, $"vid-{i}", BaseTime.AddMinutes(i));

        var results = await fixture.Sut.GetByUserIdAsync(UserId, 5, 5);

        results.Select(h => h.VideoId).Should().Equal("vid-4", "vid-3", "vid-2", "vid-1", "vid-0");
    }

    [Fact]
    public async Task AddAsync_DoesNotPersist_UntilSaveChangesAsync()
    {
        var fixture = Fixture.Init(DbContext, Db);

        await fixture.Sut.AddAsync(CreateEntry(UserId, "vid-new", BaseTime), CancellationToken.None);

        Db.Query<YoutubeDownloadHistory>().Should().BeEmpty();

        await fixture.Sut.SaveChangesAsync(CancellationToken.None);

        Db.Query<YoutubeDownloadHistory>().Should().ContainSingle();
    }

    [Fact]
    public async Task AddAsync_ThenSaveChangesAsync_PersistsRow()
    {
        var fixture = Fixture.Init(DbContext, Db);

        await fixture.Sut.AddAsync(CreateEntry(UserId, "vid-new", BaseTime), CancellationToken.None);
        await fixture.Sut.SaveChangesAsync(CancellationToken.None);

        var row = Db.Query<YoutubeDownloadHistory>().AsNoTracking().Single();
        row.UserId.Should().Be(UserId);
        row.VideoId.Should().Be("vid-new");
        row.DownloadedAt.Should().Be(BaseTime);
    }

    [Fact]
    public async Task ArchiveAsync_ArchivesOwnersRow()
    {
        var fixture = Fixture.Init(DbContext, Db).WithEntry(UserId, "vid-1", BaseTime);
        var id = fixture.SingleEntryId;

        var archived = await fixture.Sut.ArchiveAsync(id, UserId);

        archived.Should().BeTrue();
        Db.FindFresh<YoutubeDownloadHistory>(id)!.IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ArchiveAsync_ReturnsFalse_ForAnotherUsersRow_AndDoesNotMutate()
    {
        var fixture = Fixture.Init(DbContext, Db).WithEntry(UserId, "vid-1", BaseTime);
        var id = fixture.SingleEntryId;

        var archived = await fixture.Sut.ArchiveAsync(id, OtherUserId);

        archived.Should().BeFalse();
        Db.FindFresh<YoutubeDownloadHistory>(id)!.IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ArchiveAsync_ReturnsFalse_ForUnknownId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithEntry(UserId, "vid-1", BaseTime);

        var archived = await fixture.Sut.ArchiveAsync(999999, UserId);

        archived.Should().BeFalse();
        Db.Query<YoutubeDownloadHistory>().Should().ContainSingle();
    }

    private static YoutubeDownloadHistory CreateEntry(long userId, string videoId, DateTime downloadedAt) =>
        new()
        {
            UserId = userId,
            VideoId = videoId,
            VideoTitle = $"Title {videoId}",
            VideoUrl = $"https://youtube.com/watch?v={videoId}",
            DownloadedAt = downloadedAt,
        };

    private sealed class Fixture
    {
        private readonly YtDbContext _db;
        private readonly TestDb _testDb;

        public Fixture(YtDbContext dbContext, TestDb testDb)
        {
            _db = dbContext;
            _testDb = testDb;
        }

        public DownloadHistoryRepository Sut => new(_db);

        public static Fixture Init(YtDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public long SingleEntryId =>
            _testDb.Query<YoutubeDownloadHistory>().AsNoTracking().Single().Id;

        public Fixture WithEntry(long ownerUserId, string videoId, DateTime downloadedAt)
        {
            _testDb.Store(CreateEntry(ownerUserId, videoId, downloadedAt));
            return this;
        }

        public Fixture WithArchivedEntry(long ownerUserId, string videoId, DateTime downloadedAt)
        {
            var entry = CreateEntry(ownerUserId, videoId, downloadedAt);
            entry.IsArchived = true;
            _testDb.Store(entry);
            return this;
        }
    }
}
