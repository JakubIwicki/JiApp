using JiApp.Common.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockUserManager
{
	private readonly Mock<UserManager<User>> _mock;

	private MockUserManager(Mock<UserManager<User>> mock) => _mock = mock;

	public UserManager<User> Object => _mock.Object;
	public Mock<UserManager<User>> Mock => _mock;

	public static implicit operator UserManager<User>(MockUserManager m) => m.Object;

	public static MockUserManager GetSuccessful() => new(new Mock<UserManager<User>>(
		Moq.Mock.Of<IUserStore<User>>(),
		Moq.Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
		Moq.Mock.Of<IPasswordHasher<User>>(),
		Array.Empty<IUserValidator<User>>(),
		Array.Empty<IPasswordValidator<User>>(),
		Moq.Mock.Of<ILookupNormalizer>(),
		Moq.Mock.Of<IdentityErrorDescriber>(),
		Moq.Mock.Of<IServiceProvider>(),
		Moq.Mock.Of<ILogger<UserManager<User>>>()));

	public MockUserManager WithFindByIdAsync(string userId, User? user)
	{
		_mock.Setup(x => x.FindByIdAsync(userId)).ReturnsAsync(user);
		return this;
	}

	public MockUserManager WithFindByNameAsync(string username, User? user)
	{
		_mock.Setup(x => x.FindByNameAsync(username)).ReturnsAsync(user);
		return this;
	}

	public MockUserManager WithCreateAsync(string matchingPassword, IdentityResult result, Action<User>? callback = null)
	{
		var setup = _mock.Setup(x => x.CreateAsync(It.IsAny<User>(), matchingPassword));
		if (callback is not null)
			setup.Callback<User, string>((user, _) => callback(user));
		setup.ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithCreateThrowsUniqueConstraint()
	{
		var innerEx = new Microsoft.Data.Sqlite.SqliteException("UNIQUE constraint failed", 19);
		_mock.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
			.ThrowsAsync(new DbUpdateException("An error occurred while saving the entity changes", innerEx));
		return this;
	}

	public MockUserManager WithDeleteAsync(User user, IdentityResult result)
	{
		_mock.Setup(x => x.DeleteAsync(user)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithGetRolesAsync(User user, IList<string> roles)
	{
		_mock.Setup(x => x.GetRolesAsync(user)).ReturnsAsync(roles);
		return this;
	}

	public MockUserManager WithGetRolesAsyncForAny(IList<string> roles)
	{
		_mock.Setup(x => x.GetRolesAsync(It.IsAny<User>())).ReturnsAsync(roles);
		return this;
	}

	public MockUserManager WithGetUsersInRoleAsync(string role, IList<User> users)
	{
		_mock.Setup(x => x.GetUsersInRoleAsync(role)).ReturnsAsync(users);
		return this;
	}

	public MockUserManager WithIsInRoleAsync(User user, string role, bool result)
	{
		_mock.Setup(x => x.IsInRoleAsync(user, role)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithIsLockedOutAsync(User user, bool result)
	{
		_mock.Setup(x => x.IsLockedOutAsync(user)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithSetLockoutEnabledAsync(User user, bool enabled, IdentityResult result)
	{
		_mock.Setup(x => x.SetLockoutEnabledAsync(user, enabled)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithSetLockoutEndDateAsync(User user, DateTimeOffset? endDate, IdentityResult result)
	{
		_mock.Setup(x => x.SetLockoutEndDateAsync(user, endDate)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithUpdateSecurityStampAsync(User user, IdentityResult result, Action<User>? callback = null)
	{
		var setup = _mock.Setup(x => x.UpdateSecurityStampAsync(user));
		if (callback is not null)
			setup.Callback<User>(u => callback(u));
		setup.ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithUpdateSecurityStampAsyncForAny(IdentityResult result)
	{
		_mock.Setup(x => x.UpdateSecurityStampAsync(It.IsAny<User>())).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithChangePasswordAsync(User user, string currentPassword, string newPassword, IdentityResult result)
	{
		_mock.Setup(x => x.ChangePasswordAsync(user, currentPassword, newPassword)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithSetEmailAsync(User user, IdentityResult result, Action<User, string>? callback = null)
	{
		var setup = _mock.Setup(x => x.SetEmailAsync(user, It.IsAny<string>()));
		if (callback is not null)
			setup.Callback<User, string>((u, email) => callback(u, email));
		setup.ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithUpdateAsync(User user, IdentityResult result)
	{
		_mock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithGeneratePasswordResetTokenAsync(User user, string token)
	{
		_mock.Setup(x => x.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(token);
		return this;
	}

	public MockUserManager WithResetPasswordAsync(User user, string token, string newPassword, IdentityResult result)
	{
		_mock.Setup(x => x.ResetPasswordAsync(user, token, newPassword)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithAddToRoleAsync(User user, string role, IdentityResult result)
	{
		_mock.Setup(x => x.AddToRoleAsync(user, role)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithAddToRolesAsync(User user, IEnumerable<string> roles, IdentityResult result)
	{
		_mock.Setup(x => x.AddToRolesAsync(user, roles)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithAddToRolesAsyncSuccess()
	{
		_mock.Setup(x => x.AddToRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(IdentityResult.Success);
		return this;
	}

	public MockUserManager WithAddToRolesAsyncFailure(long userId, IdentityError error)
	{
		_mock.Setup(x => x.AddToRolesAsync(It.Is<User>(u => u.Id == userId), It.IsAny<IEnumerable<string>>()))
			.ReturnsAsync(IdentityResult.Failed(error));
		return this;
	}

	public MockUserManager WithRemoveFromRoleAsync(User user, string role, IdentityResult result)
	{
		_mock.Setup(x => x.RemoveFromRoleAsync(user, role)).ReturnsAsync(result);
		return this;
	}

	public MockUserManager WithUsersQueryable(IQueryable<User> queryable)
	{
		_mock.Setup(x => x.Users).Returns(queryable);
		return this;
	}

	public void VerifyDeletedUser(long userId)
	{
		_mock.Verify(x => x.DeleteAsync(It.Is<User>(u => u.Id == userId)), Times.Once);
	}

	public void VerifyUpdatedSecurityStamp(User user)
	{
		_mock.Verify(x => x.UpdateSecurityStampAsync(user), Times.Once);
	}

	public void VerifyUpdatedSecurityStamp_Once()
	{
		_mock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Once);
	}

	public void VerifyUpdatedSecurityStamp_NotCalled()
	{
		_mock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Never);
	}

	public void VerifyUpdatedSecurityStamp_Exactly(int count)
	{
		_mock.Verify(x => x.UpdateSecurityStampAsync(It.IsAny<User>()), Times.Exactly(count));
	}

	public void VerifySetLockoutEnabled()
	{
		_mock.Verify(x => x.SetLockoutEnabledAsync(It.IsAny<User>(), true), Times.Once);
	}

	public void VerifySetLockoutEndDatePermanent()
	{
		_mock.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), DateTimeOffset.MaxValue), Times.Once);
	}

	public void VerifySetLockoutEndDateCleared()
	{
		_mock.Verify(x => x.SetLockoutEndDateAsync(It.IsAny<User>(), null), Times.Once);
	}

	public void VerifySetEmailAsync_NotCalled()
	{
		_mock.Verify(x => x.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
	}

	public void VerifyDeleteAsync_Once()
	{
		_mock.Verify(x => x.DeleteAsync(It.IsAny<User>()), Times.Once);
	}

	public void VerifyGetUsersInRoleAsync_NotCalled()
	{
		_mock.Verify(x => x.GetUsersInRoleAsync(It.IsAny<string>()), Times.Never);
	}
}
