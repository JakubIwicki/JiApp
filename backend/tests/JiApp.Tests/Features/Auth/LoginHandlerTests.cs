using FluentAssertions;
using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JiApp.Tests.Features.Auth;

public class LoginHandlerTests
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
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.CheckPasswordAsync(user, "correctpassword"))
            .ReturnsAsync(true);

        var jwtTokenServiceMock = new Mock<IJwtTokenService>();
        jwtTokenServiceMock.Setup(x => x.GenerateToken(1, "testuser"))
            .Returns("jwt-token-123");

        var handler = new LoginHandler(userManagerMock.Object, jwtTokenServiceMock.Object);
        var request = new LoginRequest("testuser", "correctpassword");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("Test User");
        result.Value.Token.Should().Be("jwt-token-123");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidUsername_ReturnsFailure()
    {
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var jwtTokenServiceMock = new Mock<IJwtTokenService>();

        var handler = new LoginHandler(userManagerMock.Object, jwtTokenServiceMock.Object);
        var request = new LoginRequest("nonexistent", "password");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ReturnsFailure()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(user);
        userManagerMock.Setup(x => x.CheckPasswordAsync(user, "wrongpassword"))
            .ReturnsAsync(false);

        var jwtTokenServiceMock = new Mock<IJwtTokenService>();

        var handler = new LoginHandler(userManagerMock.Object, jwtTokenServiceMock.Object);
        var request = new LoginRequest("testuser", "wrongpassword");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }
}
