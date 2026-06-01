using JiApp.Identity.Features.Auth.Login;

namespace JiApp.Identity.Tests.Features.Auth.Login;

public class LoginValidatorTests
{
    private readonly LoginValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_username_is_empty()
    {
        var request = new LoginRequest("", "password123");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void Validate_returns_error_when_password_is_empty()
    {
        var request = new LoginRequest("testuser", "");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Validate_returns_error_when_username_exceeds_maximum_length()
    {
        var request = new LoginRequest(new string('a', 257), "password123");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Username));
    }

    [Fact]
    public void Validate_returns_error_when_password_exceeds_maximum_length()
    {
        var request = new LoginRequest("testuser", new string('p', 257));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(LoginRequest.Password));
    }

    [Fact]
    public void Validate_returns_success_for_valid_request()
    {
        var request = new LoginRequest("testuser", "password123");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
