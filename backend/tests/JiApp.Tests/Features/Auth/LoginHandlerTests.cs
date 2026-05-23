using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Login;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class LoginHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidCredentials_ReturnsSuccessWithToken()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };

        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "correctpassword", SignInResult.Success)
            .WithGenerateToken(1, "testuser", "jwt-token-123")
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "correctpassword"));

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
        var ctx = new LoginHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("nonexistent", "password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithInvalidPassword_ReturnsFailure()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };

        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "wrongpassword", SignInResult.Failed)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "wrongpassword"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Invalid username or password");
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenAccountLocked_ReturnsFailureWithLockedMessage()
    {
        var user = new User { Id = 1, UserName = "testuser", DisplayName = "Test User" };

        var ctx = new LoginHandlerFixture()
            .WithFindByNameAsync("testuser", user)
            .WithCheckPasswordSignInAsync(user, "password", SignInResult.LockedOut)
            .Build();

        var result = await ctx.Handler.HandleAsync(new LoginRequest("testuser", "password"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Account is locked. Please try again later.");
        result.Value.Should().BeNull();
    }
}