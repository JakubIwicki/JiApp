using JiApp.Common.Models;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.YtDownloader.Tests.Features.DownloadHistory;

public sealed class DownloadHistoryHandlerTests : HandlerTestBase<YtDbContext>
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;

    private static readonly DateTime FixedAt = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ReturnsHistory_ForCurrentUser_OrderedNewestFirst()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithEntry(UserId, "vid-older", FixedAt)
            .WithEntry(UserId, "vid-newer", FixedAt.AddDays(1));

        var result = await fixture.Sut.HandleAsync(new DownloadHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Items.Select(i => i.VideoId).Should().Equal("vid-newer", "vid-older");
        response.Items[0].DownloadedAt.Should().Be(FixedAt.AddDays(1));
    }

    [Fact]
    public async Task ReturnsEmpty_WhenOnlyAnotherUserHasEntries()
    {
        var fixture = Fixture.Init(DbContext, Db).WithEntry(OtherUserId, "vid-their");

        var result = await fixture.Sut.HandleAsync(new DownloadHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task OmitsArchivedEntries()
    {
        var fixture = Fixture.Init(DbContext, Db).WithArchivedEntry(UserId, "vid-archived");

        var result = await fixture.Sut.HandleAsync(new DownloadHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task AppliesLimit_ToReturnedItems()
    {
        var fixture = Fixture.Init(DbContext, Db);
        for (var i = 0; i < 20; i++)
            fixture.WithEntry(UserId, $"vid-{i}", FixedAt.AddMinutes(i));

        var result = await fixture.Sut.HandleAsync(new DownloadHistoryRequest(5));

        var response = AssertSuccess(result);
        response.Items.Should().HaveCount(5);
    }

    [Fact]
    public async Task DefaultsLimit_To10_WhenRequestNull()
    {
        var fixture = Fixture.Init(DbContext, Db);
        for (var i = 0; i < 20; i++)
            fixture.WithEntry(UserId, $"vid-{i}", FixedAt.AddMinutes(i));

        var result = await fixture.Sut.HandleAsync(new DownloadHistoryRequest(null));

        var response = AssertSuccess(result);
        response.Items.Should().HaveCount(10);
    }

    private sealed class Fixture
    {
        private readonly YtDbContext _db;
        private readonly TestDb _testDb;
        private readonly MockCurrentUserService _currentUser;

        public Fixture(YtDbContext dbContext, TestDb testDb)
        {
            _db = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful();
        }

        public DownloadHistoryHandler Sut => new(
            new DownloadHistoryRepository(_db),
            _currentUser.Object,
            Mock.Of<ILogger<DownloadHistoryHandler>>());

        public static Fixture Init(YtDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithEntry(long ownerUserId, string videoId, DateTime? downloadedAt = null)
        {
            _testDb.Store(new YoutubeDownloadHistory
            {
                UserId = ownerUserId,
                VideoId = videoId,
                VideoTitle = $"Title {videoId}",
                VideoUrl = $"https://youtube.com/watch?v={videoId}",
                DownloadedAt = downloadedAt ?? FixedAt,
            });
            return this;
        }

        public Fixture WithArchivedEntry(long ownerUserId, string videoId)
        {
            _testDb.Store(new YoutubeDownloadHistory
            {
                UserId = ownerUserId,
                VideoId = videoId,
                VideoTitle = $"Title {videoId}",
                VideoUrl = $"https://youtube.com/watch?v={videoId}",
                DownloadedAt = FixedAt,
                IsArchived = true,
            });
            return this;
        }
    }
}
