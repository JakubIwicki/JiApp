using JiApp.Identity.Models;
using JiApp.Identity.Services;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;

namespace JiApp.Identity.Tests.Mocks;

public sealed class MockRefreshTokenService : MockObject<IRefreshTokenService>
{
	public static MockRefreshTokenService GetSuccessful() => new();

	public MockRefreshTokenService WithValidateAsync(string rawToken, RefreshToken? storedToken)
	{
		Mock.Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
			.ReturnsAsync(storedToken);
		return this;
	}

	public MockRefreshTokenService WithCreateAsync(long userId, RefreshToken token)
	{
		Mock.Setup(x => x.CreateAsync(userId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(token);
		return this;
	}

	public MockRefreshTokenService WithRevokeAsync(long tokenId, bool result)
	{
		Mock.Setup(x => x.RevokeAsync(tokenId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(result);
		return this;
	}

	public MockRefreshTokenService WithRevokeAllForUserAsync(long userId)
	{
		Mock.Setup(x => x.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		return this;
	}

	public MockRefreshTokenService WithBeginTransactionAsync(IDbContextTransaction transaction)
	{
		Mock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(transaction);
		return this;
	}

	public void VerifyRevokedToken(long tokenId)
	{
		Mock.Verify(x => x.RevokeAsync(tokenId, It.IsAny<CancellationToken>()), Times.Once);
	}

	public void VerifyRevokedToken_NotCalled()
	{
		Mock.Verify(x => x.RevokeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	public void VerifyRevokedAllForUser(long userId)
	{
		Mock.Verify(x => x.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
	}

	public void VerifyRevokedAllForUser_NotCalled()
	{
		Mock.Verify(x => x.RevokeAllForUserAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
	}
}
