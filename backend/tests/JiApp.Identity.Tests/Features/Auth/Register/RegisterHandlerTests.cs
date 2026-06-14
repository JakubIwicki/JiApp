using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Register;

public class RegisterHandlerTests
{
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IUserModuleGrantService> _grantServiceMock;
    private readonly RegisterHandler _sut;

    public RegisterHandlerTests()
    {
        _userManagerMock = CreateUserManagerMock();
        _grantServiceMock = new Mock<IUserModuleGrantService>();
        var logger = Mock.Of<ILogger<RegisterHandler>>();
        _sut = new RegisterHandler(_userManagerMock.Object, _grantServiceMock.Object, logger);
    }

    [Fact]
    public async Task HandleAsync_returns_success_for_valid_registration()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "Password1"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_grants_all_modules_on_successful_registration()
    {
        const long createdUserId = 7;
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "Password1"))
            .Callback<User, string>((user, _) => user.Id = createdUserId)
            .ReturnsAsync(IdentityResult.Success);

        await _sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"));

        _grantServiceMock.Verify(x => x.GrantAllAsync(createdUserId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_does_not_grant_modules_when_registration_fails()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Passwords must have at least one uppercase ('A'-'Z')." }));

        await _sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        _grantServiceMock.Verify(x => x.GrantAllAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_returns_generic_failure_on_unique_constraint_violation()
    {
        var innerEx = new SqliteException("UNIQUE constraint failed", 19); // SQLITE_CONSTRAINT
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ThrowsAsync(new DbUpdateException("An error occurred while saving the entity changes", innerEx));

        var result = await _sut.HandleAsync(
            new RegisterRequest("existinguser", "existing@test.com", "Password1", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_when_create_fails()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Passwords must have at least one uppercase ('A'-'Z')." }));

        var result = await _sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("uppercase");
    }

    [Fact]
    public async Task HandleAsync_returns_failure_with_all_errors_when_create_fails_multiple()
    {
        _userManagerMock.Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Description = "Passwords must have at least one uppercase ('A'-'Z')." },
                new IdentityError { Description = "Passwords must have at least one digit ('0'-'9')." }));

        var result = await _sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("uppercase");
        result.Error.Should().Contain("digit");
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        return new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }
}