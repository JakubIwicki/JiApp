using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Me;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Me;

public class MeHandlerTests
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

        public Mock<ICurrentUserService> CurrentUserMock { get; } = new();
        public Mock<IUserModuleGrantService> GrantServiceMock { get; } = new();

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUserMock.Setup(x => x.UserId).Returns(userId);
            CurrentUserMock.Setup(x => x.Username).Returns("testuser");
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(_testUser);
            GrantServiceMock.Setup(x => x.GetModulesAsync(userId))
                .ReturnsAsync(["YtDownloader", "Scheduler"]);
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUserMock.Setup(x => x.UserId).Returns(userId);
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User?)null);
            return this;
        }

        public MeHandler Build() =>
            new(UserManagerMock.Object, CurrentUserMock.Object, GrantServiceMock.Object,
                Mock.Of<ILogger<MeHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsProfile_ForValidUser()
    {
        var fixture = new Fixture().WithExistingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.Username.Should().Be("testuser");
        result.Value.Modules.Should().BeEquivalentTo("YtDownloader", "Scheduler");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_ForNonexistentUser()
    {
        var fixture = new Fixture().WithMissingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
    }
}
