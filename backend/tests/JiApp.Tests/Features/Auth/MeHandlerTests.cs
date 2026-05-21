using FluentAssertions;
using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JiApp.Tests.Features.Auth;

public class MeHandlerTests
{
    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<User>>();
        var options = Mock.Of<IOptions<IdentityOptions>>();
        var hasher = Mock.Of<IPasswordHasher<User>>();
        var normalizer = Mock.Of<ILookupNormalizer>();
        var describer = Mock.Of<IdentityErrorDescriber>();
        var services = Mock.Of<IServiceProvider>();
        var logger = Mock.Of<ILogger<UserManager<User>>>();

        return new Mock<UserManager<User>>(
            store, options, hasher,
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            normalizer, describer, services, logger);
    }

    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsUserData()
    {
        var userId = 42L;
        var user = new User { Id = userId, UserName = "janek", DisplayName = "Jan Kowalski" };

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new MeHandler(userManagerMock.Object, currentUserServiceMock.Object);

        var result = await handler.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(42);
        result.Value.DisplayName.Should().Be("Jan Kowalski");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsFailure()
    {
        var userId = 999L;

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync((User?)null);

        var currentUserServiceMock = new Mock<ICurrentUserService>();
        currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        var handler = new MeHandler(userManagerMock.Object, currentUserServiceMock.Object);

        var result = await handler.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Value.Should().BeNull();
    }
}
