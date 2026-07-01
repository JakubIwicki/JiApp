using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.ResetPassword;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Admin.Users.ResetPassword;

public sealed class ResetPasswordHandlerTests
{
	private sealed class Fixture
	{
		private readonly User _testUser = new()
		{
			Id = 1,
			UserName = "testuser"
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

		public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();

		public ResetPasswordHandler Sut { get; }

		public Fixture()
		{
			Sut = new ResetPasswordHandler(UserManagerMock.Object, RefreshTokenServiceMock.Object);
		}

		public Fixture WithSuccessfulReset()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("1"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(_testUser))
				.ReturnsAsync("reset-token");
			UserManagerMock.Setup(x => x.ResetPasswordAsync(_testUser, "reset-token", "NewPassword1"))
				.ReturnsAsync(IdentityResult.Success);
			RefreshTokenServiceMock.Setup(x => x.RevokeAllForUserAsync(1))
				.Returns(Task.CompletedTask);
			return this;
		}

		public Fixture WithFailingReset(string errorDescription)
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("1"))
				.ReturnsAsync(_testUser);
			UserManagerMock.Setup(x => x.GeneratePasswordResetTokenAsync(_testUser))
				.ReturnsAsync("reset-token");
			UserManagerMock.Setup(x => x.ResetPasswordAsync(_testUser, "reset-token", "weak"))
				.ReturnsAsync(IdentityResult.Failed(
					new IdentityError { Description = errorDescription }));
			return this;
		}

		public Fixture WithNonexistentUser()
		{
			UserManagerMock.Setup(x => x.FindByIdAsync("999"))
				.ReturnsAsync((User?)null);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_AndRevokesTokens_WhenPasswordReset()
	{
		var fixture = new Fixture().WithSuccessfulReset();

		var result = await fixture.Sut.HandleAsync(1, new ResetPasswordRequest("NewPassword1"));

		AssertSuccess(result);
		fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(1), Times.Once);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentUser();

		var result = await fixture.Sut.HandleAsync(999, new ResetPasswordRequest("NewPassword1"));

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_AndDoesNotRevokeTokens_WhenResetPasswordFails()
	{
		var fixture = new Fixture().WithFailingReset("Passwords must be at least 6 characters.");

		var result = await fixture.Sut.HandleAsync(1, new ResetPasswordRequest("weak"));

		result.IsSuccess.Should().BeFalse();
		result.ErrorCategory.Should().Be(ResultCategories.Validation);
		fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAllForUserAsync(It.IsAny<long>()), Times.Never);
	}
}
