using System;
using System.Threading.Tasks;
using FluentAssertions;
using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class RegisterHandlerTests
{
    [Fact]
    public async Task HandleAsync_WithValidRequest_ReturnsSuccess()
    {
        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new RegisterHandler(userManagerMock.Object, LoggerMock.Of<RegisterHandler>());
        var request = new RegisterRequest("testuser", "test@example.com", "password", "Test User");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateUsername_ReturnsFailure()
    {
        var existingUser = new User { UserName = "existing", Email = "existing@example.com" };

        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync("existing"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterHandler(userManagerMock.Object, LoggerMock.Of<RegisterHandler>());
        var request = new RegisterRequest("existing", "new@example.com", "password", "Existing User");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Username already taken");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User { UserName = "other", Email = "test@example.com" };

        var userManagerMock = UserManagerMock.GetSuccessful();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterHandler(userManagerMock.Object, LoggerMock.Of<RegisterHandler>());
        var request = new RegisterRequest("newuser", "test@example.com", "password", "New User");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already taken");
    }
}
