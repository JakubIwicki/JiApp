using FluentAssertions;
using JiApp.Api.Features.Auth.Login;
using Xunit;

namespace JiApp.Tests.Features.Auth;

public class LoginValidatorTests
{
    private readonly LoginValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new LoginRequest("validuser", "pass1234");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyUsername_Fails()
    {
        var request = new LoginRequest("", "pass1234");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
    }

    [Fact]
    public void Validate_EmptyPassword_Fails()
    {
        var request = new LoginRequest("validuser", "");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Password");
    }
}