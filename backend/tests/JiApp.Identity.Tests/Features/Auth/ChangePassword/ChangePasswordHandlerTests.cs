using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Auth.ChangePassword;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.ChangePassword;

public class ChangePasswordHandlerTests
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
            return this;
        }

        public Fixture WithSuccessfulPasswordChange()
        {
            UserManagerMock.Setup(x => x.ChangePasswordAsync(_testUser, "OldPass1", "NewPass1"))
                .ReturnsAsync(IdentityResult.Success);
            return this;
        }

        public Fixture WithFailingPasswordChange()
        {
            UserManagerMock.Setup(x => x.ChangePasswordAsync(_testUser, "WrongPass", "NewPass1"))
                .ReturnsAsync(IdentityResult.Failed(
                    new IdentityError { Description = "Incorrect password." }));
            return this;
        }

        public Fixture WithMissingUser(long userId = 999)
        {
            CurrentUserMock.Setup(x => x.UserId).Returns(userId);
            UserManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User?)null);
            return this;
        }

        public ChangePasswordHandler Build() =>
            new(UserManagerMock.Object, CurrentUserMock.Object, Mock.Of<ILogger<ChangePasswordHandler>>());
    }

    [Fact]
    public async Task HandleAsync_ReturnsSuccess_WhenPasswordChanged()
    {
        var fixture = new Fixture().WithExistingUser().WithSuccessfulPasswordChange();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new ChangePasswordRequest("OldPass1", "NewPass1"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenCurrentPasswordWrong()
    {
        var fixture = new Fixture().WithExistingUser().WithFailingPasswordChange();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new ChangePasswordRequest("WrongPass", "NewPass1"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Incorrect password");
        result.ErrorCategory.Should().Be(ResultCategories.Validation);
    }

    [Fact]
    public async Task HandleAsync_ReturnsFailure_WhenUserNotFound()
    {
        var fixture = new Fixture().WithMissingUser();
        var sut = fixture.Build();

        var result = await sut.HandleAsync(
            new ChangePasswordRequest("OldPass1", "NewPass1"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("User not found");
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }
}
