using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Me;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
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

        public Mock<UserManager<User>> UserManagerMock { get; } = new(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());

        public MockCurrentUserService CurrentUserMock { get; } = new();
        public Mock<IUserModuleGrantService> GrantServiceMock { get; } = new();

        public MeHandler Sut { get; }

        public Fixture()
        {
            Sut = new MeHandler(
                UserManagerMock.Object, CurrentUserMock.Mock.Object, GrantServiceMock.Object,
                Mock.Of<ILogger<MeHandler>>());
        }

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUserMock.WithReturning(userId);
            CurrentUserMock.Mock.Setup(x => x.Username).Returns("testuser");
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(_testUser);
            GrantServiceMock.Setup(x => x.GetModulesAsync(userId))
                .ReturnsAsync(["YtDownloader", "Scheduler"]);
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUserMock.WithReturning(userId);
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User?)null);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsProfile_ForValidUser()
    {
        var fixture = new Fixture().WithExistingUser();

        var result = await fixture.Sut.HandleAsync();

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.Username.Should().Be("testuser");
        result.Value.Email.Should().Be("test@test.com");
        result.Value.Modules.Should().BeEquivalentTo("YtDownloader", "Scheduler");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithMissingUser();

        var result = await fixture.Sut.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }
}
