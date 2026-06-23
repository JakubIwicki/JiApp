using JiApp.Identity.Features.Auth.UpdateProfile;

namespace JiApp.Identity.Tests.Features.Auth.UpdateProfile;

public class UpdateProfileValidatorTests
{
    private readonly UpdateProfileValidator _sut = new();

    [Fact]
    public void Validate_returns_error_when_display_name_is_empty()
    {
        var request = new UpdateProfileRequest("", "test@test.com");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.DisplayName));
    }

    [Fact]
    public void Validate_returns_error_when_display_name_exceeds_max_length()
    {
        var request = new UpdateProfileRequest(new string('A', 51), "test@test.com");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.DisplayName));
    }

    [Fact]
    public void Validate_returns_error_when_email_is_empty()
    {
        var request = new UpdateProfileRequest("Test User", "");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Email));
    }

    [Fact]
    public void Validate_returns_error_when_email_format_is_invalid()
    {
        var request = new UpdateProfileRequest("Test User", "not-an-email");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateProfileRequest.Email));
    }

    [Fact]
    public void Validate_returns_success_for_valid_request()
    {
        var request = new UpdateProfileRequest("Test User", "test@test.com");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
