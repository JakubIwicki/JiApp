using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Fixtures;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class MeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsUserData()
    {
        const long userId = 42L;
        var user = new User { Id = userId, UserName = "janek", DisplayName = "Jan Kowalski" };

        var ctx = new MeHandlerFixture()
            .WithUserId(userId)
            .WithFindByIdAsync(userId.ToString(CultureInfo.InvariantCulture), user)
            .Build();

        var result = await ctx.Handler.HandleAsync();

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(42);
        result.Value.DisplayName.Should().Be("Jan Kowalski");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WhenUserNotFound_ReturnsFailure()
    {
        const long userId = 999L;

        var ctx = new MeHandlerFixture()
            .WithUserId(userId)
            .WithFindByIdAsync(userId.ToString(CultureInfo.InvariantCulture), null)
            .Build();

        var result = await ctx.Handler.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Value.Should().BeNull();
    }
}