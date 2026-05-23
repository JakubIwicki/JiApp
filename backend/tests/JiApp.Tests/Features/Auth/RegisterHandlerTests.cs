using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class RegisterHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsSuccess()
    {
        var ctx = new RegisterHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .WithAnyFindByEmailAsync(null)
            .WithAnyCreateAsync(IdentityResult.Success)
            .Build();

        var result = await ctx.Handler.HandleAsync(new RegisterRequest("testuser", "test@example.com", "password", "Test User"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateUsername_ReturnsFailure()
    {
        var existingUser = new User { UserName = "existing", Email = "existing@example.com" };

        var ctx = new RegisterHandlerFixture()
            .WithFindByNameAsync("existing", existingUser)
            .Build();

        var result = await ctx.Handler.HandleAsync(new RegisterRequest("existing", "new@example.com", "password", "Existing User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Username already taken");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User { UserName = "other", Email = "test@example.com" };

        var ctx = new RegisterHandlerFixture()
            .WithAnyFindByNameAsync(null)
            .WithFindByEmailAsync("test@example.com", existingUser)
            .Build();

        var result = await ctx.Handler.HandleAsync(new RegisterRequest("newuser", "test@example.com", "password", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already taken");
    }
}
