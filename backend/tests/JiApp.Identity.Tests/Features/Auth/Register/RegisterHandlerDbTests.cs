using JiApp.Common;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using IdentityDbContext = JiApp.Identity.Persistence.IdentityDbContext;

namespace JiApp.Identity.Tests.Features.Auth.Register;

/// <summary>
/// Real-context suite for RegisterHandler: every test drives a real UserManager
/// over a real IdentityDbContext so the persistence paths — identity-column
/// generation, the DB unique-constraint handling, and role-assignment
/// compensation — are exercised, not mocked. G9.2.
/// </summary>
public sealed class RegisterHandlerDbTests : HandlerTestBase<IdentityDbContext>
{
    private const string ValidUsername = "newuser";
    private const string ValidEmail = "new@test.com";
    private const string ValidPassword = "Password1!";
    private const string ValidDisplayName = "New User";
    private const string WeakPassword = "weak";

    [Fact]
    public async Task Register_CreatesUser_AssignsDefaultRole_AndGeneratesId()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var request = new RegisterRequest(ValidUsername, ValidEmail, ValidPassword, ValidDisplayName);

        var result = await fixture.Sut.HandleAsync(request, CancellationToken.None);

        var response = AssertSuccess(result);
        response.UserId.Should().BeGreaterThan(0);
        var user = Db.FindFresh<User>(response.UserId);
        user.Should().NotBeNull();
        user!.UserName.Should().Be(ValidUsername);
        user.Email.Should().Be(ValidEmail);
        user.DisplayName.Should().Be(ValidDisplayName);
        var guestRole = DbContext.Roles.Single(r => r.Name == RoleNames.Guest);
        var roleAssignment = DbContext.UserRoles.Single();
        roleAssignment.UserId.Should().Be(response.UserId);
        roleAssignment.RoleId.Should().Be(guestRole.Id);
    }

    [Fact]
    public async Task Register_DuplicateUsername_ReturnsGenericFailure_WithoutPersistingDuplicate()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var first = new RegisterRequest("duplicate", "alice@test.com", ValidPassword, "Alice");
        var duplicate = new RegisterRequest("duplicate", "bob@test.com", ValidPassword, "Bob");
        var firstResult = await fixture.Sut.HandleAsync(first, CancellationToken.None);
        AssertSuccess(firstResult);

        var result = await fixture.Sut.HandleAsync(duplicate, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        (await DbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsFailure_WithoutPersistingDuplicate()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var first = new RegisterRequest("alice", "duplicate@test.com", ValidPassword, "Alice");
        var duplicate = new RegisterRequest("bob", "duplicate@test.com", ValidPassword, "Bob");
        var firstResult = await fixture.SutWithEmailUniqueness.HandleAsync(first, CancellationToken.None);
        AssertSuccess(firstResult);

        var result = await fixture.SutWithEmailUniqueness.HandleAsync(duplicate, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        (await DbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Register_RoleAssignmentFailure_CompensatesByDeletingUser()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var request = new RegisterRequest(ValidUsername, ValidEmail, ValidPassword, ValidDisplayName);

        var result = await fixture.SutWithFailingAccessService.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        (await DbContext.Users.CountAsync()).Should().Be(0);
        (await DbContext.UserRoles.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsGenericMessage_NoUserPersisted()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var request = new RegisterRequest(ValidUsername, ValidEmail, WeakPassword, ValidDisplayName);

        var result = await fixture.Sut.HandleAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
        (await DbContext.Users.CountAsync()).Should().Be(0);
    }

    private static UserManager<User> CreateRealUserManager(IdentityDbContext context)
    {
        // No IUserValidator: the default UserValidator pre-checks username
        // uniqueness via FindByNameAsync and returns the enumerating
        // "Username 'x' is already taken." before the DB is reached.
        // RegisterHandler's design — and G9.2 — targets the DB unique-constraint
        // path (DbUpdateException -> SqliteErrorCode 19 on the UserNameIndex).
        var store = new UserStore<User, IdentityRole<long>, IdentityDbContext, long>(context);
        return new UserManager<User>(
            store,
            Options.Create(CreatePasswordOptions()),
            new PasswordHasher<User>(),
            [],
            [new PasswordValidator<User>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);
    }

    private static UserManager<User> CreateEmailUniqueUserManager(IdentityDbContext context)
    {
        // Production sets RequireUniqueEmail=true, and the real schema's
        // EmailIndex is NOT unique, so duplicate-email rejection comes from the
        // UserValidator's store lookup (FindByEmailAsync), not a DB constraint.
        var store = new UserStore<User, IdentityRole<long>, IdentityDbContext, long>(context);
        var options = CreatePasswordOptions();
        options.User.RequireUniqueEmail = true;
        return new UserManager<User>(
            store,
            Options.Create(options),
            new PasswordHasher<User>(),
            [new UserValidator<User>()],
            [new PasswordValidator<User>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new ServiceCollection().BuildServiceProvider(),
            NullLogger<UserManager<User>>.Instance);
    }

    private static RoleManager<IdentityRole<long>> CreateRealRoleManager(IdentityDbContext context) =>
        new(new RoleStore<IdentityRole<long>, IdentityDbContext, long>(context),
            [new RoleValidator<IdentityRole<long>>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            NullLogger<RoleManager<IdentityRole<long>>>.Instance);

    private static IUserAccessService CreateFailingAccessService()
    {
        var mock = new Mock<IUserAccessService>();
        mock.Setup(x => x.AssignDefaultRoleAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));
        return mock.Object;
    }

    private static IdentityOptions CreatePasswordOptions()
    {
        var options = new IdentityOptions();
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 1;
        return options;
    }

    private sealed class Fixture
    {
        private readonly TestDb _testDb;
        private readonly UserManager<User> _userManager;
        private readonly UserManager<User> _emailUniqueUserManager;
        private readonly RoleManager<IdentityRole<long>> _roleManager;

        private Fixture(IdentityDbContext dbContext, TestDb testDb)
        {
            _testDb = testDb;
            _userManager = CreateRealUserManager(dbContext);
            _emailUniqueUserManager = CreateEmailUniqueUserManager(dbContext);
            _roleManager = CreateRealRoleManager(dbContext);
            SeedGuestRole();
        }

        public RegisterHandler Sut =>
            new(_userManager, new UserAccessService(_userManager, _roleManager), NullLogger<RegisterHandler>.Instance);

        public RegisterHandler SutWithEmailUniqueness =>
            new(_emailUniqueUserManager, new UserAccessService(_emailUniqueUserManager, _roleManager),
                NullLogger<RegisterHandler>.Instance);

        public RegisterHandler SutWithFailingAccessService =>
            new(_userManager, CreateFailingAccessService(), NullLogger<RegisterHandler>.Instance);

        public static Fixture Init(IdentityDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        private void SeedGuestRole()
        {
            var guestRole = new IdentityRole<long>(RoleNames.Guest)
            {
                NormalizedName = new UpperInvariantLookupNormalizer().NormalizeName(RoleNames.Guest)
            };
            _testDb.Store(guestRole);
        }
    }
}
