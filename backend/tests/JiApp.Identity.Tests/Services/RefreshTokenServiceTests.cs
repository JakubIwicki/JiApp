using JiApp.Common.Models;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Identity.Tests.Services;

public class RefreshTokenServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _db;
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new IdentityDbContext(options);
        _db.Database.EnsureCreated();

        // Seed a user for FK constraint
        _db.Users.Add(new User
        {
            Id = 1,
            UserName = "test",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        });
        _db.SaveChanges();

        _sut = new RefreshTokenService(_db, 7);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task CreateAsync_generates_unique_tokens()
    {
        var t1 = await _sut.CreateAsync(1);
        var t2 = await _sut.CreateAsync(1);
        t1.Token.Should().NotBe(t2.Token);
    }

    [Fact]
    public async Task ValidateAsync_returns_entity_for_valid_token()
    {
        var created = await _sut.CreateAsync(1);
        var validated = await _sut.ValidateAsync(created.Token);
        validated.Should().NotBeNull();
        validated!.UserId.Should().Be(1);
    }

    [Fact]
    public async Task ValidateAsync_returns_null_for_invalid_token()
    {
        var result = await _sut.ValidateAsync("not-a-valid-token");
        result.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_returns_revoked_token_after_revoke()
    {
        var created = await _sut.CreateAsync(1);
        await _sut.RevokeAsync(created.Id);
        var result = await _sut.ValidateAsync(created.Token);
        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAsync_does_not_throw_for_nonexistent_id()
    {
        var act = () => _sut.RevokeAsync(99999);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task RevokeAllForUserAsync_revokes_all_user_tokens()
    {
        var t1 = await _sut.CreateAsync(1);
        var t2 = await _sut.CreateAsync(1);
        await _sut.RevokeAllForUserAsync(1);
        (await _sut.ValidateAsync(t1.Token))!.IsRevoked.Should().BeTrue();
        (await _sut.ValidateAsync(t2.Token))!.IsRevoked.Should().BeTrue();
    }
}