using FluentAssertions;
using JiApp.Api.Features.Auth.Register;

namespace JiApp.Tests.Features.Auth;

public class RegisterValidatorTests
{
    private readonly RegisterValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new RegisterRequest("validuser", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUsername_Fails()
    {
        var request = new RegisterRequest("", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_UsernameLessThan3Chars_Fails()
    {
        var request = new RegisterRequest("ab", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_UsernameGreaterThan50Chars_Fails()
    {
        var request = new RegisterRequest(
            new string('a', 51),
            "user@example.com",
            "pass1234",
            "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_UsernameWithInvalidChars_Fails()
    {
        var request = new RegisterRequest("user name", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_UsernameWithSpecialChars_Fails()
    {
        var request = new RegisterRequest("user@name", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_UsernameWithExclamation_Fails()
    {
        var request = new RegisterRequest("user!name", "user@example.com", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_EmptyEmail_Fails()
    {
        var request = new RegisterRequest("validuser", "", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_Fails()
    {
        var request = new RegisterRequest("validuser", "not-an-email", "pass1234", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Validate_EmptyPassword_Fails()
    {
        var request = new RegisterRequest("validuser", "user@example.com", "", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_PasswordLessThan4Chars_Fails()
    {
        var request = new RegisterRequest("validuser", "user@example.com", "abc", "Display Name");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Validate_EmptyDisplayName_Fails()
    {
        var request = new RegisterRequest("validuser", "user@example.com", "pass1234", "");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }

    [Fact]
    public void Validate_DisplayNameGreaterThan50Chars_Fails()
    {
        var request = new RegisterRequest(
            "validuser",
            "user@example.com",
            "pass1234",
            new string('a', 51));
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "DisplayName");
    }
}
