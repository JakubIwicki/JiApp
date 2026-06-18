using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Tests.Services;

public sealed class RefreshTokenServiceTests : IDisposable
{
    private sealed class Fixture : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly IdentityDbContext _db;

        public RefreshTokenService Sut { get; }

        public Fixture()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            var options = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(_connection)
                .Options;

            _db = new IdentityDbContext(options);
            _db.Database.EnsureCreated();

            _db.Users.Add(new User
            {
                Id = 1,
                UserName = "test",
                Email = "test@test.com",
                SecurityStamp = "stamp",
                ConcurrencyStamp = "concurrency"
            });
            _db.SaveChanges();

            Sut = new RefreshTokenService(_db, 7);
        }

        public void Dispose()
        {
            _db.Dispose();
            _connection.Dispose();
        }
    }

    private readonly Fixture _fixture;

    public RefreshTokenServiceTests()
    {
        _fixture = new Fixture();
    }

    public void Dispose()
    {
        _fixture.Dispose();
    }

    [Fact]
    public async Task CreateAsync_GeneratesUniqueTokens()
    {
        var t1 = await _fixture.Sut.CreateAsync(1);
        var t2 = await _fixture.Sut.CreateAsync(1);

        t1.Token.Should().NotBe(t2.Token);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsEntity_ForValidToken()
    {
        var created = await _fixture.Sut.CreateAsync(1);

        var validated = await _fixture.Sut.ValidateAsync(created.Token);

        validated.Should().NotBeNull();
        validated!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_ReturnsNull_ForInvalidToken()
    {
        var result = await _fixture.Sut.ValidateAsync("not-a-valid-token");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ReturnsRevokedToken_AfterRevoke()
    {
        var created = await _fixture.Sut.CreateAsync(1);
        await _fixture.Sut.RevokeAsync(created.Id);

        var result = await _fixture.Sut.ValidateAsync(created.Token);

        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_DoesNotThrow_ForNonexistentId()
    {
        var act = () => _fixture.Sut.RevokeAsync(99999);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_RevokesAllUserTokens()
    {
        var t1 = await _fixture.Sut.CreateAsync(1);
        var t2 = await _fixture.Sut.CreateAsync(1);

        await _fixture.Sut.RevokeAllForUserAsync(1);

        (await _fixture.Sut.ValidateAsync(t1.Token))!.IsRevoked.Should().BeTrue();
        (await _fixture.Sut.ValidateAsync(t2.Token))!.IsRevoked.Should().BeTrue();
    }
}
