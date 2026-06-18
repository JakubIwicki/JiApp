using JiApp.Common.Models;
using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Register;

public sealed class RegisterHandlerTests
{
    private sealed class Fixture
    {
        public Mock<UserManager<User>> UserManagerMock { get; } = new(
            Mock.Of<IUserStore<User>>(),
            Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            Array.Empty<IUserValidator<User>>(),
            Array.Empty<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            Mock.Of<IdentityErrorDescriber>(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());

        public Mock<IUserModuleGrantService> GrantServiceMock { get; } = new();

        public RegisterHandler Sut { get; }

        public Fixture()
        {
            Sut = new RegisterHandler(UserManagerMock.Object, GrantServiceMock.Object, Mock.Of<ILogger<RegisterHandler>>());
        }

        public Fixture WithSuccessfulCreate(long userId = 7)
        {
            UserManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<User>(), "Password1"))
                .Callback<User, string>((user, _) => user.Id = userId)
                .ReturnsAsync(IdentityResult.Success);
            return this;
        }

        public Fixture WithFailingCreate(string errorDescription)
        {
            UserManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = errorDescription }));
            return this;
        }

        public Fixture WithCreateFailingMultiple()
        {
            UserManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<User>(), "weak"))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = "Passwords must have at least one uppercase ('A'-'Z')." },
                    new IdentityError { Description = "Passwords must have at least one digit ('0'-'9')." }));
            return this;
        }

        public Fixture WithUniqueConstraintViolation()
        {
            var innerEx = new SqliteException("UNIQUE constraint failed", 19);
            UserManagerMock
                .Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                .ThrowsAsync(new DbUpdateException("An error occurred while saving the entity changes", innerEx));
            return this;
        }

        public Fixture WithFailingGrantAllocation(long userId)
        {
            GrantServiceMock
                .Setup(x => x.GrantAllAsync(userId))
                .ThrowsAsync(new InvalidOperationException("DB unavailable"));
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_ForValidRegistration()
    {
        var fixture = new Fixture().WithSuccessfulCreate();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"));

        AssertSuccess(result);
    }

    [Fact]
    public async Task HandleAsync_GrantsAllModules_OnSuccessfulRegistration()
    {
        const long createdUserId = 7;
        var fixture = new Fixture().WithSuccessfulCreate(createdUserId);

        await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"));

        fixture.GrantServiceMock.Verify(x => x.GrantAllAsync(createdUserId), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotGrantModules_WhenRegistrationFails()
    {
        var fixture = new Fixture().WithFailingCreate("Passwords must have at least one uppercase ('A'-'Z').");

        await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        fixture.GrantServiceMock.Verify(x => x.GrantAllAsync(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsGenericFailure_OnUniqueConstraintViolation()
    {
        var fixture = new Fixture().WithUniqueConstraintViolation();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("existinguser", "existing@test.com", "Password1", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenCreateFails()
    {
        var fixture = new Fixture().WithFailingCreate("Passwords must have at least one uppercase ('A'-'Z').");

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("uppercase");
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailureWithAllErrors_WhenCreateFailsMultiple()
    {
        var fixture = new Fixture().WithCreateFailingMultiple();

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "weak", "New User"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("uppercase");
        result.Error.Should().Contain("digit");
    }

    [Fact]
    public async Task HandleAsync_CompensatesUserDeletion_WhenGrantAllocationFails()
    {
        const long createdUserId = 9;
        var fixture = new Fixture()
            .WithSuccessfulCreate(createdUserId)
            .WithFailingGrantAllocation(createdUserId);

        var result = await fixture.Sut.HandleAsync(
            new RegisterRequest("newuser", "new@test.com", "Password1", "New User"));

        fixture.UserManagerMock.Verify(
            x => x.DeleteAsync(It.Is<User>(u => u.Id == createdUserId)),
            Times.Once);
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Registration failed");
    }
}
