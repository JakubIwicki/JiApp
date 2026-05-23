using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Services;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class LoginHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(user);

        var signInManagerMock = SignInManagerMock.GetSuccessful(userManagerMock);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "correctpassword", true))
            .ReturnsAsync(SignInResult.Success);

        var jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();
        jwtTokenServiceMock.Setup(x => x.GenerateToken(1, "testuser"))
            .Returns("jwt-token-123");

        var handler = new LoginHandler(signInManagerMock.Object, jwtTokenServiceMock.Object, LoggerMock.Of<LoginHandler>());
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
        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        var signInManagerMock = SignInManagerMock.GetSuccessful(userManagerMock);

        var jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();

        var handler = new LoginHandler(signInManagerMock.Object, jwtTokenServiceMock.Object, LoggerMock.Of<LoginHandler>());
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
        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(user);

        var signInManagerMock = SignInManagerMock.GetSuccessful(userManagerMock);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "wrongpassword", true))
            .ReturnsAsync(SignInResult.Failed);

        var jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();

        var handler = new LoginHandler(signInManagerMock.Object, jwtTokenServiceMock.Object, LoggerMock.Of<LoginHandler>());
        var request = new LoginRequest("testuser", "wrongpassword");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenAccountLocked_ReturnsFailureWithLockedMessage()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };
        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync("testuser"))
            .ReturnsAsync(user);

        var signInManagerMock = SignInManagerMock.GetSuccessful(userManagerMock);
        signInManagerMock.Setup(x => x.CheckPasswordSignInAsync(user, "password", true))
            .ReturnsAsync(SignInResult.LockedOut);

        var jwtTokenServiceMock = JwtTokenServiceMock.GetSuccessful();

        var handler = new LoginHandler(signInManagerMock.Object, jwtTokenServiceMock.Object, LoggerMock.Of<LoginHandler>());
        var request = new LoginRequest("testuser", "password");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
        result.Value.Should().BeNull();
    }
}
