using JiApp.Common.Models;
using JiApp.YtDownloader.Features.ArchiveDownload;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Tests.Features.ArchiveDownload;

public sealed class ArchiveDownloadHandlerTests : HandlerTestBase<YtDbContext>
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;

    [Fact]
    public async Task Archives_WhenOwnedByCaller()
    {
        var fixture = Fixture.Init(DbContext, Db).WithDownloadEntry(UserId);
        var id = fixture.DownloadEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveDownloadRequest(id));

        AssertSuccess(result).Should().BeTrue();
        fixture.FindEntry(id).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsNotFound_ForUnknownId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithDownloadEntry(UserId);
        var id = fixture.DownloadEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveDownloadRequest(id + 1));

        AssertNotFound(result);
        fixture.FindEntry(id).IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenOwnedByAnotherUser()
    {
        var fixture = Fixture.Init(DbContext, Db).WithDownloadEntry(OtherUserId);
        var id = fixture.DownloadEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveDownloadRequest(id));

        AssertNotFound(result);
        fixture.FindEntry(id).IsArchived.Should().BeFalse();
    }

    private sealed class Fixture
    {
        private readonly YtDbContext _db;
        private readonly TestDb _testDb;
        private readonly MockCurrentUserService _currentUser;

        public Fixture(YtDbContext dbContext, TestDb testDb, long userId)
        {
            _db = dbContext;
            _testDb = testDb;
            _currentUser = new MockCurrentUserService().WithReturning(userId);
        }

        public ArchiveDownloadHandler Sut => new(new DownloadHistoryRepository(_db), _currentUser.Object);

        public static Fixture Init(YtDbContext dbContext, TestDb testDb, long userId = UserId) =>
            new(dbContext, testDb, userId);

        public Fixture WithDownloadEntry(long ownerUserId, string videoId = "vid")
        {
            _testDb.Store(new YoutubeDownloadHistory
            {
                UserId = ownerUserId,
                VideoId = videoId,
                VideoTitle = $"Title {videoId}",
                VideoUrl = $"https://youtube.com/watch?v={videoId}",
                DownloadedAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            return this;
        }

        public long DownloadEntryId =>
            _testDb.Query<YoutubeDownloadHistory>().AsNoTracking().Single().Id;

        public YoutubeDownloadHistory FindEntry(long id) =>
            _testDb.Query<YoutubeDownloadHistory>().AsNoTracking().Single(e => e.Id == id);
    }
}
