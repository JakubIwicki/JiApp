using JiApp.Identity.Features.Auth.Logout;
using JiApp.Identity.Models;
using JiApp.Identity.Tests.Mocks;
using Microsoft.Extensions.Logging;
using Moq;

namespace JiApp.Identity.Tests.Features.Auth.Logout;

public sealed class LogoutHandlerTests
{
    private sealed class Fixture
    {
        public MockRefreshTokenService RefreshTokenDouble { get; } = MockRefreshTokenService.GetSuccessful();

        public LogoutHandler Sut { get; }

        public Fixture()
        {
            Sut = new LogoutHandler(RefreshTokenDouble.Object, Mock.Of<ILogger<LogoutHandler>>());
        }

        public Fixture WithValidToken(string rawToken = "valid-token")
        {
            RefreshTokenDouble.WithValidateAsync(rawToken,
                new RefreshToken { Id = 10, Token = "raw-token", UserId = 1 });
            return this;
        }

        public Fixture WithAlreadyRevokedToken(string rawToken = "already-revoked")
        {
            RefreshTokenDouble.WithValidateAsync(rawToken,
                new RefreshToken { Id = 10, Token = "raw-token", UserId = 1, IsRevoked = true });
            return this;
        }

        public Fixture WithNonexistentToken(string rawToken = "nonexistent-token")
        {
            RefreshTokenDouble.WithValidateAsync(rawToken, null);
            return this;
        }
    }

    [Fact]
    public async Task HandleAsync_RevokesToken_WhenValid()
    {
        var fixture = new Fixture().WithValidToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("valid-token"), CancellationToken.None);

        fixture.RefreshTokenDouble.VerifyRevokedToken(10);
    }

    [Fact]
    public async Task HandleAsync_RevokesAlreadyRevokedToken_Idempotently()
    {
        var fixture = new Fixture().WithAlreadyRevokedToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("already-revoked"), CancellationToken.None);

        // RevokeAsync is idempotent; the call is made regardless since
        // ValidateAsync now returns revoked tokens for reuse detection
        fixture.RefreshTokenDouble.VerifyRevokedToken(10);
    }

    [Fact]
    public async Task HandleAsync_DoesNotRevoke_WhenTokenDoesNotExist()
    {
        var fixture = new Fixture().WithNonexistentToken();

        await fixture.Sut.HandleAsync(new LogoutRequest("nonexistent-token"), CancellationToken.None);

        fixture.RefreshTokenDouble.VerifyRevokedToken_NotCalled();
    }
}
