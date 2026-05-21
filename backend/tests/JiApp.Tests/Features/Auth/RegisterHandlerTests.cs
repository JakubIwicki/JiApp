using FluentAssertions;
using JiApp.Api.Features.Auth.Register;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace JiApp.Tests.Features.Auth;

public class RegisterHandlerTests
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
    public async Task HandleAsync_WithValidRequest_ReturnsSuccess()
    {
        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var handler = new RegisterHandler(userManagerMock.Object);
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

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync("existing"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterHandler(userManagerMock.Object);
        var request = new RegisterRequest("existing", "new@example.com", "password", "Existing User");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Username already taken");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsFailure()
    {
        var existingUser = new User { UserName = "other", Email = "test@example.com" };

        var userManagerMock = CreateUserManagerMock();
        userManagerMock.Setup(x => x.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);
        userManagerMock.Setup(x => x.FindByEmailAsync("test@example.com"))
            .ReturnsAsync(existingUser);

        var handler = new RegisterHandler(userManagerMock.Object);
        var request = new RegisterRequest("newuser", "test@example.com", "password", "New User");

        var result = await handler.HandleAsync(request);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Email already taken");
    }
}
