using JiApp.Identity.Services;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockJwtTokenService : MockObject<IJwtTokenService>
{
	public static MockJwtTokenService GetSuccessful() => new();

	public MockJwtTokenService WithGenerateToken(
		long userId, string username, IEnumerable<string> roles, IEnumerable<string> permissions, string securityStamp,
		string token)
	{
		Mock.Setup(x => x.GenerateToken(userId, username, roles, permissions, securityStamp)).Returns(token);
		return this;
	}

	public MockJwtTokenService WithGenerateTokenAny(string token)
	{
		Mock.Setup(x => x.GenerateToken(
			It.IsAny<long>(), It.IsAny<string>(),
			It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
			It.IsAny<string>())).Returns(token);
		return this;
	}

	public void VerifyGeneratedToken(long userId, string username,
		IEnumerable<string> roles, IEnumerable<string> permissions, string securityStamp)
	{
		Mock.Verify(x => x.GenerateToken(userId, username, roles, permissions, securityStamp), Times.Once);
	}

	public void VerifyGeneratedToken(long userId, string username, string securityStamp)
	{
		Mock.Verify(x => x.GenerateToken(
			userId, username,
			It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(),
			securityStamp), Times.Once);
	}

	public void VerifyGeneratedTokenWithRolesAndPermissions(
		long userId, string username,
		string[] roles, string[] permissions, string securityStamp)
	{
		Mock.Verify(x => x.GenerateToken(
			userId, username,
			It.Is<IEnumerable<string>>(r => r.SequenceEqual(roles)),
			It.Is<IEnumerable<string>>(p => p.SequenceEqual(permissions)),
			securityStamp), Times.Once);
	}

}
