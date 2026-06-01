using JiApp.Identity.Features.Auth.Logout;
using JiApp.Identity.Models;
using JiApp.Identity.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Logout;

public class LogoutHandlerTests
{
    private readonly Mock<IRefreshTokenService> _refreshTokenServiceMock;
    private readonly LogoutHandler _sut;

    public LogoutHandlerTests()
    {
        _refreshTokenServiceMock = new Mock<IRefreshTokenService>();
        var logger = Mock.Of<ILogger<LogoutHandler>>();
        _sut = new LogoutHandler(_refreshTokenServiceMock.Object, logger);
    }

    [Fact]
    public async Task HandleAsync_revokes_token_when_valid()
    {
        var storedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1 };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("valid-token"))
            .ReturnsAsync(storedToken);

        await _sut.HandleAsync(new LogoutRequest("valid-token"));

        _refreshTokenServiceMock.Verify(x => x.RevokeAsync(10), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_revokes_already_revoked_token_idempotently()
    {
        var revokedToken = new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true };
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("already-revoked"))
            .ReturnsAsync(revokedToken);

        await _sut.HandleAsync(new LogoutRequest("already-revoked"));

        // RevokeAsync is idempotent; the call is made regardless since
        // ValidateAsync now returns revoked tokens for reuse detection
        _refreshTokenServiceMock.Verify(x => x.RevokeAsync(10), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_does_not_revoke_when_token_does_not_exist()
    {
        _refreshTokenServiceMock.Setup(x => x.ValidateAsync("nonexistent-token"))
            .ReturnsAsync((RefreshToken?)null);

        await _sut.HandleAsync(new LogoutRequest("nonexistent-token"));

        _refreshTokenServiceMock.Verify(x => x.RevokeAsync(It.IsAny<long>()), Times.Never);
    }
}