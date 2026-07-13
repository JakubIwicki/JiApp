using JiApp.Identity.Features.Auth.Logout;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Logout;

public sealed class LogoutHandlerTests
{
    private sealed class Fixture
    {
        public Mock<IRefreshTokenService> RefreshTokenServiceMock { get; } = new();

        public LogoutHandler Sut { get; }

        public Fixture()
        {
            Sut = new LogoutHandler(RefreshTokenServiceMock.Object, Mock.Of<ILogger<LogoutHandler>>());
        }

        public Fixture WithValidToken(string rawToken = "valid-token")
        {
            RefreshTokenServiceMock
                .Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RefreshToken { Id = 10, Token = "raw-token", UserId = 1 });
            return this;
        }

        public Fixture WithAlreadyRevokedToken(string rawToken = "already-revoked")
        {
            RefreshTokenServiceMock
                .Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true });
            return this;
        }

        public Fixture WithNonexistentToken(string rawToken = "nonexistent-token")
        {
            RefreshTokenServiceMock
                .Setup(x => x.ValidateAsync(rawToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync((RefreshToken?)null);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_RevokesToken_WhenValid()
    {
        var fixture = new Fixture().WithValidToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("valid-token"), CancellationToken.None);

        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_RevokesAlreadyRevokedToken_Idempotently()
    {
        var fixture = new Fixture().WithAlreadyRevokedToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("already-revoked"), CancellationToken.None);

        // RevokeAsync is idempotent; the call is made regardless since
        // ValidateAsync now returns revoked tokens for reuse detection
        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAsync(10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotRevoke_WhenTokenDoesNotExist()
    {
        var fixture = new Fixture().WithNonexistentToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("nonexistent-token"), CancellationToken.None);

        fixture.RefreshTokenServiceMock.Verify(x => x.RevokeAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
