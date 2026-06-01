using JiApp.Identity.Features.Auth.Refresh;

namespace JiApp.Identity.Tests.Features.Auth.Refresh;

public class RefreshValidatorTests
{
    private readonly RefreshValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_token_is_empty()
    {
        var request = new RefreshRequest("");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RefreshRequest.RefreshToken));
    }

    [Fact]
    public void Validate_returns_success_for_valid_token()
    {
        var request = new RefreshRequest("valid-refresh-token");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}