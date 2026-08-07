using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.ListUsers;
using JiApp.Testing.Common.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using IdentityDbContext = JiApp.Identity.Persistence.IdentityDbContext;

namespace JiApp.Identity.Tests.Features.Admin.Users.ListUsers;

/// <summary>
/// Real-context suite for ListUsersHandler: the paged user query and the batch
/// roles query run against a real IdentityDbContext so the join shape and the
/// query count (G10.4) are exercised, not mocked.
/// </summary>
public sealed class ListUsersHandlerTests : HandlerTestBase<IdentityDbContext>
{
    private const string SeededRole = "User";

    [Fact]
    public async Task HandleAsync_ReturnsAllUsers_WhenNoSearchProvided()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(5);

        var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_ReturnsRolesForPagedUsers()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(5);

        var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().OnlyContain(u => u.Roles.SequenceEqual(new[] { SeededRole }));
    }

    [Fact]
    public async Task HandleAsync_ReturnsEmptyRoles_WhenUserHasNoRole()
    {
        var fixture = Fixture.Init(DbContext).WithUserWithoutRole(1);

        var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Single().Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task HandleAsync_ReturnsPaginatedResults()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(5);

        var result = await fixture.Sut.HandleAsync(null, 1, 2, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task HandleAsync_ReturnsIsLockedOutTrue_WhenUserIsLockedOut()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(1).WithUserLockedOut(1);

        var result = await fixture.Sut.HandleAsync(null, 1, 20, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Single().IsLockedOut.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ReturnsCorrectPage_WhenMoreUsersThanPageSize()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(10);

        var result = await fixture.Sut.HandleAsync(null, 2, 3, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(10);
        result.Value.Users[0].Id.Should().Be(4);
    }

    [Fact]
    public async Task HandleAsync_ClampsPageSize_ToMaximum()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(150);

        var result = await fixture.Sut.HandleAsync(null, page: 1, pageSize: 500, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().HaveCount(100);
        result.Value.TotalCount.Should().Be(150);
    }

    [Fact]
    public async Task HandleAsync_ClampsPage_ToMinimum()
    {
        var fixture = Fixture.Init(DbContext).WithUsers(5);

        var result = await fixture.Sut.HandleAsync(null, page: 0, pageSize: 20, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Users.Should().HaveCount(5);
    }

    [Fact]
    public async Task HandleAsync_QueryCount_DoesNotGrowWithPageSize()
    {
        using var counting = SqliteQueryCountFixture<IdentityDbContext>.Create();
        var fixture = Fixture.Init(counting.Db).WithUsers(30);
        counting.Interceptor.Reset();

        await fixture.Sut.HandleAsync(null, 1, 5, CancellationToken.None);
        var smallPageCount = counting.Interceptor.Count;

        counting.Interceptor.Reset();
        await fixture.Sut.HandleAsync(null, 1, 50, CancellationToken.None);
        var largePageCount = counting.Interceptor.Count;

        // A per-user role lookup would scale the count with the page; the batch
        // roles query keeps it flat.
        largePageCount.Should().Be(smallPageCount);
        largePageCount.Should().Be(3); // count + paged users + batch roles
    }

    private static UserManager<User> CreateRealUserManager(IdentityDbContext context) =>
        new(new UserStore<User, IdentityRole<long>, IdentityDbContext, long>(context),
            Options.Create(new IdentityOptions()),
            new PasswordHasher<User>(),
            [],
            [],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);

    private sealed class Fixture
    {
        private readonly IdentityDbContext _db;
        private readonly UserManager<User> _userManager;

        public ListUsersHandler Sut { get; }

        private Fixture(IdentityDbContext db)
        {
            _db = db;
            _userManager = CreateRealUserManager(db);
            Sut = new ListUsersHandler(_userManager, db);
        }

        public static Fixture Init(IdentityDbContext db) => new(db);

        public Fixture WithUsers(int count)
        {
            var role = new IdentityRole<long>(SeededRole);
            _db.Roles.Add(role);
            _db.SaveChanges();

            for (var i = 1; i <= count; i++)
            {
                _db.Users.Add(new User
                {
                    Id = i,
                    UserName = $"user{i}",
                    Email = $"user{i}@test.com",
                    DisplayName = $"User {i}"
                });
                _db.UserRoles.Add(new IdentityUserRole<long> { UserId = i, RoleId = role.Id });
            }

            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithUserWithoutRole(long userId)
        {
            _db.Users.Add(new User
            {
                Id = userId,
                UserName = $"user{userId}",
                Email = $"user{userId}@test.com",
                DisplayName = $"User {userId}"
            });
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }

        public Fixture WithUserLockedOut(long userId)
        {
            var user = _db.Users.Find(userId)!;
            user.LockoutEnd = DateTimeOffset.MaxValue;
            _db.SaveChanges();
            _db.ChangeTracker.Clear();
            return this;
        }
    }
}
