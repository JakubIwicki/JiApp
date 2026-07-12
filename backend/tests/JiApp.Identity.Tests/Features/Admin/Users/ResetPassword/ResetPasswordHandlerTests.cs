using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Features.Admin.Users.ResetPassword;
using JiApp.Identity.Tests.Mocks;
using Microsoft.AspNetCore.Identity;
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

		public MockUserManager UserManagerDouble { get; } = MockUserManager.GetSuccessful();
		public MockRefreshTokenService RefreshTokenDouble { get; } = MockRefreshTokenService.GetSuccessful();

		public ResetPasswordHandler Sut { get; }

		public Fixture()
		{
			Sut = new ResetPasswordHandler(UserManagerDouble, RefreshTokenDouble.Object);
		}

		public Fixture WithSuccessfulReset()
		{
			UserManagerDouble.WithFindByIdAsync("1", _testUser);
			UserManagerDouble.WithGeneratePasswordResetTokenAsync(_testUser, "reset-token");
			UserManagerDouble.WithResetPasswordAsync(_testUser, "reset-token", "NewPassword1", IdentityResult.Success);
			RefreshTokenDouble.WithRevokeAllForUserAsync(1);
			return this;
		}

		public Fixture WithFailingReset(string errorDescription)
		{
			UserManagerDouble.WithFindByIdAsync("1", _testUser);
			UserManagerDouble.WithGeneratePasswordResetTokenAsync(_testUser, "reset-token");
			UserManagerDouble.WithResetPasswordAsync(_testUser, "reset-token", "weak",
				IdentityResult.Failed(new IdentityError { Description = errorDescription }));
			return this;
		}

		public Fixture WithNonexistentUser()
		{
			UserManagerDouble.WithFindByIdAsync("999", null);
			return this;
		}
	}

	[Fact]
	public async Task HandleAsync_ReturnsSuccess_AndRevokesTokens_WhenPasswordReset()
	{
		var fixture = new Fixture().WithSuccessfulReset();

		var result = await fixture.Sut.HandleAsync(1, new ResetPasswordRequest("NewPassword1"), CancellationToken.None);

		AssertSuccess(result);
		fixture.RefreshTokenDouble.VerifyRevokedAllForUser(1);
	}

	[Fact]
	public async Task HandleAsync_ReturnsNotFound_WhenUserDoesNotExist()
	{
		var fixture = new Fixture().WithNonexistentUser();

		var result = await fixture.Sut.HandleAsync(999, new ResetPasswordRequest("NewPassword1"), CancellationToken.None);

		AssertNotFound(result);
	}

	[Fact]
	public async Task HandleAsync_ReturnsValidationFailure_AndDoesNotRevokeTokens_WhenResetPasswordFails()
	{
		var fixture = new Fixture().WithFailingReset("Passwords must be at least 6 characters.");

		var result = await fixture.Sut.HandleAsync(1, new ResetPasswordRequest("weak"), CancellationToken.None);

		result.IsSuccess.Should().BeFalse();
		result.ErrorCategory.Should().Be(ResultCategories.Validation);
		fixture.RefreshTokenDouble.VerifyRevokedAllForUser_NotCalled();
	}
}
