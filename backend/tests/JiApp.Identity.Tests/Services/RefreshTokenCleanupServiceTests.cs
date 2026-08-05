using JiApp.Common.Models;
using JiApp.Identity.Models;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using JiApp.Testing.Common.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Tests.Services;

public sealed class RefreshTokenCleanupServiceTests
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly ServiceProvider _provider;

        public FakeTimeProvider Clock { get; }
        public RefreshTokenCleanupService Service { get; }
        public IdentityDbContext Db { get; }

        public Fixture()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            Clock = new FakeTimeProvider(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton(Clock);
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(_connection));
            _provider = services.BuildServiceProvider();

            Db = _provider.GetRequiredService<IdentityDbContext>();
            Db.Database.EnsureCreated();
            Db.Users.Add(new User
            {
                Id = 1,
                UserName = "test",
                Email = "test@test.com",
                SecurityStamp = "stamp",
                ConcurrencyStamp = "concurrency"
            });
            Db.SaveChanges();

            Service = new RefreshTokenCleanupService(
                _provider.GetRequiredService<IServiceScopeFactory>(),
                Clock,
                _provider.GetRequiredService<ILogger<RefreshTokenCleanupService>>());
        }

        public RefreshToken AddToken(DateTime expiresAt, bool isRevoked = false)
        {
            var token = new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N"),
                UserId = 1,
                ExpiresAt = expiresAt,
                CreatedAt = Clock.GetUtcNow().UtcDateTime,
                IsRevoked = isRevoked
            };
            Db.RefreshTokens.Add(token);
            Db.SaveChanges();
            return token;
        }

        public void Dispose()
        {
            Db.Dispose();
            _provider.Dispose();
            _connection.Dispose();
        }
    }

    [Fact]
    public async Task Sweep_WhenClockAdvancesPastExpiry_DeletesExpiredUnrevokedTokens()
    {
        using var fixture = new Fixture();
        var live = fixture.AddToken(fixture.Clock.GetUtcNow().UtcDateTime.AddDays(10));
        var expired = fixture.AddToken(fixture.Clock.GetUtcNow().UtcDateTime.AddDays(-1));
        fixture.Clock.Advance(TimeSpan.FromDays(7));

        await fixture.Service.SweepExpiredAsync(CancellationToken.None);

        fixture.Db.RefreshTokens.SingleOrDefault(t => t.Id == expired.Id).Should().BeNull();
        fixture.Db.RefreshTokens.SingleOrDefault(t => t.Id == live.Id).Should().NotBeNull();
    }
}
