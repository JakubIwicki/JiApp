using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.UpdateProfile;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.UpdateProfile;

public class UpdateProfileHandlerTests
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

        public Mock<ICurrentUserService> CurrentUserMock { get; } = new();

        public Fixture WithExistingUser(long userId = 1)
        {
            CurrentUserMock.Setup(x => x.UserId).Returns(userId);
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(_testUser);
            UserManagerMock.Setup(x => x.UpdateAsync(_testUser))
                .ReturnsAsync(IdentityResult.Success);
            return this;
        }

        public Fixture WithSuccessfulEmailChange()
        {
            UserManagerMock.Setup(x => x.SetEmailAsync(_testUser, It.IsAny<string>()))
                .Callback<User, string>((u, email) => u.Email = email)
                .ReturnsAsync(IdentityResult.Success);
            return this;
        }

        public Fixture WithDuplicateEmailFailure()
        {
            UserManagerMock.Setup(x => x.SetEmailAsync(_testUser, It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Code = "DuplicateEmail", Description = "Email 'new@test.com' is already taken." }));
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUserMock.Setup(x => x.UserId).Returns(userId);
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User?)null);
            return this;
        }

        public UpdateProfileHandler Build() =>
            new(UserManagerMock.Object, CurrentUserMock.Object, Mock.Of<ILogger<UpdateProfileHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsUpdatedProfile_OnSuccess()
    {
        var fixture = new Fixture().WithExistingUser().WithSuccessfulEmailChange();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().Be(1);
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.Username.Should().Be("testuser");
        result.Value.Email.Should().Be("new@test.com");
    }

    [Fact]
    public async Task HandleAsync_SkipsEmailUpdate_WhenEmailUnchanged()
    {
        var fixture = new Fixture().WithExistingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new UpdateProfileRequest("New Name", "old@test.com"));

        result.IsSuccess.Should().BeTrue();
        fixture.UserManagerMock.Verify(
            x => x.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenUserNotFound()
    {
        var fixture = new Fixture().WithMissingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenEmailDuplicate()
    {
        var fixture = new Fixture().WithExistingUser().WithDuplicateEmailFailure();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new UpdateProfileRequest("New Name", "new@test.com"));

        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(ResultCategories.Conflict);
        result.Error.Should().Contain("already taken");
    }
}
