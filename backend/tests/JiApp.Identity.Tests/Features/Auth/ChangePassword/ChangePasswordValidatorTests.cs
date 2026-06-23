using JiApp.Identity.Features.Auth.ChangePassword;

namespace JiApp.Identity.Tests.Features.Auth.ChangePassword;

public class ChangePasswordValidatorTests
{
    private readonly ChangePasswordValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_current_password_is_empty()
    {
        var request = new ChangePasswordRequest("", "NewPass1");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.CurrentPassword));
    }

    [Fact]
    public void Validate_returns_error_when_new_password_is_empty()
    {
        var request = new ChangePasswordRequest("OldPass1", "");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_returns_error_when_new_password_is_below_minimum_length()
    {
        var request = new ChangePasswordRequest("OldPass1", "Sh0rt");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_returns_error_when_new_password_missing_uppercase()
    {
        var request = new ChangePasswordRequest("OldPass1", "newpass1");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_returns_error_when_new_password_missing_digit()
    {
        var request = new ChangePasswordRequest("OldPass1", "NewPassWord");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_returns_success_for_valid_request()
    {
        var request = new ChangePasswordRequest("OldPass1", "NewPass1");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
