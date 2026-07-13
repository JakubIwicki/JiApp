using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.UpdateProfile;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.UpdateProfile;

public sealed class UpdateProfileHandlerTests
{
    private sealed class Fixture
    {
        private readonly User _testUser = new()
        {
            Id = 1,
            UserName = "testuser",
            DisplayName = "Test User",
            Email = "old@test.com",
            SecurityStamp = "stamp",
            ConcurrencyStamp = "concurrency"
        };

        public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
        public MockCurrentUserService CurrentUser { get; } = MockCurrentUserService.GetSuccessful();

        public UpdateProfileHandler Sut =>
            new(UserManagerDouble.Object, CurrentUser.Object, Mock.Of<ILogger<UpdateProfileHandler>>());

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUser.WithReturning(userId);
            UserManagerDouble.WithFindByIdAsync(userId.ToString(), _testUser);
            UserManagerDouble.WithUpdateAsync(_testUser, IdentityResult.Success);
            return this;
        }

        public Fixture WithSuccessfulEmailChange()
        {
            UserManagerDouble.WithSetEmailAsync(_testUser, IdentityResult.Success,
                callback: (u, email) => u.Email = email);
            return this;
        }

        public Fixture WithDuplicateEmailFailure()
        {
            UserManagerDouble.WithSetEmailAsync(_testUser,
                IdentityResult.Failed(
                    new IdentityError { Code = "DuplicateEmail", Description = "Email 'new@test.com' is already taken." }));
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
    public async Task HandleAsync_WithValidProfile_ReturnsUpdatedProfile()
    {
        var fixture = new Fixture().WithExistingUser().WithSuccessfulEmailChange();

        var result = await fixture.Sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"), CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.Username.Should().Be("testuser");
        result.Value.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task HandleAsync_WithUnchangedEmail_SkipsEmailUpdate()
    {
        var fixture = new Fixture().WithExistingUser();

        var result = await fixture.Sut.HandleAsync(
            new UpdateProfileRequest("New Name", "old@test.com"), CancellationToken.None);

        AssertSuccess(result);
        fixture.UserManagerDouble.VerifySetEmailAsync_NotCalled();
    }

    [Fact]
    public async Task HandleAsync_WithMissingUser_ReturnsNotFound()
    {
        var fixture = new Fixture().WithMissingUser();

        var result = await fixture.Sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"), CancellationToken.None);

        AssertFailure(result, ResultCategories.NotFound);
        result.Error.Should().Be("User not found");
    }

    [Fact]
    public async Task HandleAsync_WithDuplicateEmail_ReturnsConflict()
    {
        var fixture = new Fixture().WithExistingUser().WithDuplicateEmailFailure();

        var result = await fixture.Sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"), CancellationToken.None);

        AssertFailure(result, ResultCategories.Conflict);
        result.Error.Should().Contain("already taken");
    }
}
