using JiApp.Identity.Services;

namespace JiApp.Identity.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _sut = new(
        "this-is-a-test-key-that-is-at-least-32-bytes-long!",
        "JiApp-Identity",
        "jiapp-gateway",
        15);

    [Fact]
    public void GenerateToken_returns_valid_jwt()
    {
        var token = _sut.GenerateToken(1, "testuser");

        token.Should().NotBeNullOrEmpty();
        _sut.IsTokenValid(token).Should().BeTrue();
    }

    [Fact]
    public void IsTokenValid_returns_false_for_garbage_token()
    {
        _sut.IsTokenValid("not-a-jwt").Should().BeFalse();
    }

    [Fact]
    public void GetUsernameFromToken_returns_correct_username()
    {
        var token = _sut.GenerateToken(42, "jakub");
        var username = _sut.GetUsernameFromToken(token);
        username.Should().Be("jakub");
    }

    [Fact]
    public void GetUserIdFromToken_returns_correct_user_id()
    {
        var token = _sut.GenerateToken(42, "jakub");
        var userId = _sut.GetUserIdFromToken(token);
        userId.Should().Be(42);
    }

    [Fact]
    public void GenerateToken_produces_different_tokens_for_different_users()
    {
        var t1 = _sut.GenerateToken(1, "alice");
        var t2 = _sut.GenerateToken(2, "bob");

        t1.Should().NotBe(t2);
    }

    [Fact]
    public void IsTokenValid_returns_false_for_expired_token()
    {
        var expiredSut = new JwtTokenService(
            "this-is-a-test-key-that-is-at-least-32-bytes-long!",
            "JiApp-Identity",
            "jiapp-gateway",
            -1); // already expired

        var token = expiredSut.GenerateToken(1, "testuser");
        _sut.IsTokenValid(token).Should().BeFalse();
    }
}