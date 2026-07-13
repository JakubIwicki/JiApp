using JiApp.Common.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockSignInManager
{
	private readonly Mock<SignInManager<User>> _mock;

	private MockSignInManager(Mock<SignInManager<User>> mock) => _mock = mock;

	public SignInManager<User> Object => _mock.Object;
	public Mock<SignInManager<User>> Mock => _mock;

	public static implicit operator SignInManager<User>(MockSignInManager m) => m.Object;

	public static MockSignInManager GetSuccessful(
		UserManager<User> userManager,
		IHttpContextAccessor httpContextAccessor)
	{
		return new(new Mock<SignInManager<User>>(
			userManager,
			httpContextAccessor,
			Moq.Mock.Of<IUserClaimsPrincipalFactory<User>>(),
			Moq.Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
			Moq.Mock.Of<ILogger<SignInManager<User>>>(),
			Moq.Mock.Of<IAuthenticationSchemeProvider>(),
			Moq.Mock.Of<IUserConfirmation<User>>()));
	}

	public MockSignInManager WithCheckPasswordSignInAsync(User user, string password, SignInResult result)
	{
		_mock.Setup(x => x.CheckPasswordSignInAsync(user, password, true)).ReturnsAsync(result);
		return this;
	}

	public MockSignInManager WithCheckPasswordSignInAsyncSuccess(User user, string password)
	{
		return WithCheckPasswordSignInAsync(user, password, SignInResult.Success);
	}

	public MockSignInManager WithCheckPasswordSignInAsyncFailed(User user, string password)
	{
		return WithCheckPasswordSignInAsync(user, password, SignInResult.Failed);
	}

	public MockSignInManager WithCheckPasswordSignInAsyncLockedOut(User user)
	{
		_mock.Setup(x => x.CheckPasswordSignInAsync(user, It.IsAny<string>(), true))
			.ReturnsAsync(SignInResult.LockedOut);
		return this;
	}
}
