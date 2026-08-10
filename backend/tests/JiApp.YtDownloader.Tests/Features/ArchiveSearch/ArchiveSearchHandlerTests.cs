using JiApp.Common.Models;
using JiApp.YtDownloader.Features.ArchiveSearch;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Tests.Features.ArchiveSearch;

public sealed class ArchiveSearchHandlerTests : HandlerTestBase<YtDbContext>
{
    private const long UserId = 1L;
    private const long OtherUserId = 2L;

    [Fact]
    public async Task Archives_WhenOwnedByCaller()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSearchEntry(UserId);
        var id = fixture.SearchEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveSearchRequest(id));

        AssertSuccess(result).Should().BeTrue();
        fixture.FindEntry(id).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task ReturnsNotFound_ForUnknownId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSearchEntry(UserId);
        var id = fixture.SearchEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveSearchRequest(id + 1));

        AssertNotFound(result);
        fixture.FindEntry(id).IsArchived.Should().BeFalse();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenOwnedByAnotherUser()
    {
        var fixture = Fixture.Init(DbContext, Db).WithSearchEntry(OtherUserId);
        var id = fixture.SearchEntryId;

        var result = await fixture.Sut.HandleAsync(new ArchiveSearchRequest(id));

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

        public ArchiveSearchHandler Sut => new(new SearchHistoryRepository(_db), _currentUser.Object);

        public static Fixture Init(YtDbContext dbContext, TestDb testDb, long userId = UserId) =>
            new(dbContext, testDb, userId);

        public Fixture WithSearchEntry(long ownerUserId, string searchText = "test")
        {
            _testDb.Store(new YoutubeSearchHistory
            {
                UserId = ownerUserId,
                SearchText = searchText,
                SearchedAt = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            });
            return this;
        }

        public long SearchEntryId =>
            _testDb.Query<YoutubeSearchHistory>().AsNoTracking().Single().Id;

        public YoutubeSearchHistory FindEntry(long id) =>
            _testDb.Query<YoutubeSearchHistory>().AsNoTracking().Single(e => e.Id == id);
    }
}
