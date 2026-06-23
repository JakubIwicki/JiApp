using JiApp.Common.Models;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.YtDownloader.Tests.Repositories;

public sealed class AssistantUsageRepositoryTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly YtDbContext _db;

        public Fixture()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            var options = new DbContextOptionsBuilder<YtDbContext>()
                .UseSqlite(_connection)
                .Options;
            _db = new YtDbContext(options);
            _db.Database.EnsureCreated();
        }

        public IAssistantUsageRepository Sut => new AssistantUsageRepository(_db);

        public Fixture WithUsage(long userId, DateOnly usageDateUtc, int count)
        {
            _db.AssistantDailyUsage.Add(new AssistantDailyUsage
            {
                UserId = userId,
                UsageDateUtc = usageDateUtc,
                Count = count,
            });
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public async Task<int> GetCountAsync(long userId, DateOnly usageDateUtc)
        {
            var row = await _db.AssistantDailyUsage
                .AsNoTracking()
                .SingleOrDefaultAsync(u => u.UserId == userId && u.UsageDateUtc == usageDateUtc);
            return row?.Count ?? 0;
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Close();
            _connection.Dispose();
        }
    }

    [Fact]
    public async Task TryConsumeAsync_WithFirstCallsUpToLimit_ReturnsTrueEachTime()
    {
        using var fixture = new Fixture();
        const long userId = 1L;
        const int limit = 3;

        List<bool> results = [];
        for (var i = 0; i < limit; i++)
            results.Add(await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None));

        results.Should().AllBeEquivalentTo(true);
    }

    [Fact]
    public async Task TryConsumeAsync_WithCallBeyondLimit_ReturnsFalse()
    {
        using var fixture = new Fixture();
        const long userId = 1L;
        const int limit = 3;

        for (var i = 0; i < limit; i++)
            await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None);

        var beyondLimit = await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None);

        beyondLimit.Should().BeFalse();
    }

    [Fact]
    public async Task TryConsumeAsync_AfterRejection_CountEqualsLimit()
    {
        using var fixture = new Fixture();
        const long userId = 1L;
        const int limit = 3;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (var i = 0; i < limit; i++)
            await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None);
        await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None);

        var count = await fixture.GetCountAsync(userId, today);

        count.Should().Be(limit);
    }

    [Fact]
    public async Task TryConsumeAsync_WithDifferentUsers_IsIsolated()
    {
        using var fixture = new Fixture();
        const long userA = 1L;
        const long userB = 2L;
        const int limit = 2;

        await fixture.Sut.TryConsumeAsync(userA, limit, CancellationToken.None);
        await fixture.Sut.TryConsumeAsync(userA, limit, CancellationToken.None);
        var userAExhausted = await fixture.Sut.TryConsumeAsync(userA, limit, CancellationToken.None);

        var userBFirst = await fixture.Sut.TryConsumeAsync(userB, limit, CancellationToken.None);

        userAExhausted.Should().BeFalse();
        userBFirst.Should().BeTrue();
    }

    [Fact]
    public async Task TryConsumeAsync_WithYesterdayAtLimit_DoesNotBlockToday()
    {
        const long userId = 1L;
        const int limit = 3;
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        using var fixture = new Fixture().WithUsage(userId, yesterday, limit);

        var todayFirst = await fixture.Sut.TryConsumeAsync(userId, limit, CancellationToken.None);

        todayFirst.Should().BeTrue();
    }
}
