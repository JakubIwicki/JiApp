using JiApp.Common.Constants;
using JiApp.Identity.Features.Auth.Register;

namespace JiApp.Identity.Tests.Features.Auth.Register;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_email_is_empty()
    {
        var request = new RegisterRequest("testuser", "", "Password1", "Test User");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Email));
    }

    [Fact]
    public void Validate_returns_error_when_username_is_empty()
    {
        var request = new RegisterRequest("", "test@test.com", "Password1", "Test User");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Username));
    }

    [Fact]
    public void Validate_returns_error_when_password_is_below_minimum_length()
    {
        var request = new RegisterRequest("testuser", "test@test.com", "Sh0rt", "Test User");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Validate_returns_error_when_password_lacks_uppercase_letter()
    {
        var request = new RegisterRequest("testuser", "test@test.com", "password1", "Test User");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterRequest.Password));
    }

    [Fact]
    public void Validate_returns_success_for_valid_request()
    {
        var request = new RegisterRequest("testuser", "test@test.com", "Password1", "Test User");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}