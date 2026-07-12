using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.ChangePassword;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.ChangePassword;

public sealed class ChangePasswordHandlerTests
{
    private sealed class Fixture
    {
        private readonly User _testUser = new()
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "test@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public MockCurrentUserService CurrentUser { get; } = MockCurrentUserService.GetSuccessful();

        public ChangePasswordHandler Sut =>
            new(UserManagerDouble.Object, CurrentUser.Object, Mock.Of<ILogger<ChangePasswordHandler>>());

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUser.WithReturning(userId);
            UserManagerDouble.WithFindByIdAsync(userId.ToString(), _testUser);
            return this;
        }

        public Fixture WithSuccessfulPasswordChange()
        {
            UserManagerDouble.WithChangePasswordAsync(_testUser, "OldPass1", "NewPass1", IdentityResult.Success);
            return this;
        }

        public Fixture WithFailingPasswordChange()
        {
            UserManagerDouble.WithChangePasswordAsync(_testUser, "WrongPass", "NewPass1",
                IdentityResult.Failed(new IdentityError { Description = "Incorrect password." }));
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUser.WithReturning(userId);
            UserManagerDouble.WithFindByIdAsync(userId.ToString(), null);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_WithValidPasswords_ReturnsSuccess()
    {
        var fixture = new Fixture().WithExistingUser().WithSuccessfulPasswordChange();

        var result = await fixture.Sut.HandleAsync(
            new ChangePasswordRequest("OldPass1", "NewPass1"), CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_WithWrongCurrentPassword_ReturnsValidationFailure()
    {
        var fixture = new Fixture().WithExistingUser().WithFailingPasswordChange();

        var result = await fixture.Sut.HandleAsync(
            new ChangePasswordRequest("WrongPass", "NewPass1"), CancellationToken.None);

        AssertFailure(result, ResultCategories.Validation);
        result.Error.Should().Contain("Incorrect password");
    }

    [Fact]
    public async Task HandleAsync_WithMissingUser_ReturnsNotFound()
    {
        var fixture = new Fixture().WithMissingUser();

        var result = await fixture.Sut.HandleAsync(
            new ChangePasswordRequest("OldPass1", "NewPass1"), CancellationToken.None);

        AssertFailure(result, ResultCategories.NotFound);
        result.Error.Should().Be("User not found");
    }
}
