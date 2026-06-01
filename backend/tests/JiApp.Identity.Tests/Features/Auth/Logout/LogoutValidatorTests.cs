using JiApp.Identity.Features.Auth.Logout;

namespace JiApp.Identity.Tests.Features.Auth.Logout;

public class LogoutValidatorTests
{
    private readonly LogoutValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_token_is_empty()
    {
        var request = new LogoutRequest("");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LogoutRequest.RefreshToken));
    }

    [Fact]
    public void Validate_returns_error_when_token_exceeds_maximum_length()
    {
        var request = new LogoutRequest(new string('x', 513));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LogoutRequest.RefreshToken));
    }

    [Fact]
    public void Validate_returns_success_for_valid_token()
    {
        var request = new LogoutRequest("valid-refresh-token");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
