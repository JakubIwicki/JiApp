using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Me;
using JiApp.Identity.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Me;

public sealed class MeHandlerTests
{
    private sealed class Fixture
    {
        private readonly User _testUser = new()
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public MockCurrentUserService CurrentUserMock { get; } = new();
        public MockUserAccessService AccessServiceDouble { get; } = MockUserAccessService.GetSuccessful();

        public MeHandler Sut { get; }

        public Fixture()
        {
            Sut = new MeHandler(
                UserManagerDouble, CurrentUserMock.Object, AccessServiceDouble.Object,
                Mock.Of<ILogger<MeHandler>>());
        }

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUserMock.WithReturning(userId);
            CurrentUserMock.WithUsername("testuser");
            UserManagerDouble.WithFindByIdAsync(userId.ToString(), _testUser);
            UserManagerDouble.WithGetRolesAsync(_testUser, ["User"]);
            AccessServiceDouble.WithGetEffectivePermissionsAsync(userId, ["ytdownloader.access", "scheduler.access"]);
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUserMock.WithReturning(userId);
            UserManagerDouble.WithFindByIdAsync(userId.ToString(), null);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsProfile_ForValidUser()
    {
        var fixture = new Fixture().WithExistingUser();

        var result = await fixture.Sut.HandleAsync(CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.Username.Should().Be("testuser");
        result.Value.Email.Should().Be("test@test.com");
        result.Value.Roles.Should().BeEquivalentTo("User");
        result.Value.Permissions.Should().BeEquivalentTo("ytdownloader.access", "scheduler.access");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithMissingUser();

        var result = await fixture.Sut.HandleAsync(CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }
}
