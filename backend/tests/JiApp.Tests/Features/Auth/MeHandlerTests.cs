using System;
using System.Globalization;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Me;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class MeHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidToken_ReturnsUserData()
    {
        const long userId = 42L;
        var user = new User { Id = userId, UserName = "janek", DisplayName = "Jan Kowalski" };

        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture)))
            .ReturnsAsync(user);

        var currentUserServiceMock = CurrentUserServiceMock.GetSuccessful(userId: 42L);

        var handler = new MeHandler(userManagerMock.Object, currentUserServiceMock.Object, LoggerMock.Of<MeHandler>());

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

        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString(CultureInfo.InvariantCulture)))
            .ReturnsAsync((User?)null);

        var currentUserServiceMock = CurrentUserServiceMock.GetSuccessful(userId: 999L);

        var handler = new MeHandler(userManagerMock.Object, currentUserServiceMock.Object, LoggerMock.Of<MeHandler>());

        var result = await handler.HandleAsync();

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.Value.Should().BeNull();
    }
}
