using JiApp.Identity.Features.Auth.ChangePassword;

namespace JiApp.Identity.Tests.Features.Auth.ChangePassword;

public sealed class ChangePasswordValidatorTests : ValidatorTestBase
{
    private sealed class Fixture
    {
        public ChangePasswordValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyCurrentPassword_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("", "NewPass1");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.CurrentPassword));
    }

    [Fact]
    public void Validate_WithEmptyNewPassword_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("OldPass1", "");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordBelowMinimumLength_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("OldPass1", "Sh0rt");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordMissingUppercase_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("OldPass1", "newpass1");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_WithNewPasswordMissingDigit_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("OldPass1", "NewPassWord");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordRequest.NewPassword));
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new ChangePasswordRequest("OldPass1", "NewPass1");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
