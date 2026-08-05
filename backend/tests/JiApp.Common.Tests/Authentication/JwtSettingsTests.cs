using JiApp.Common.Authentication;

namespace JiApp.Common.Tests.Authentication;

public sealed class JwtSettingsTests
{
    private static JwtSettings ValidSettings => new()
    {
        Key = "test-key-at-least-32-characters!",
        Issuer = "test-issuer",
        Audience = "test-audience"
    };

    [Fact]
    public void Validate_ReturnsNoErrors_WhenAllValuesAreValid()
    {
        var errors = ValidSettings.Validate();

        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsError_WhenKeyIsShorterThan32Characters()
    {
        var settings = ValidSettings;
        settings.Key = "too-short";

        var errors = settings.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("Jwt:Key must be at least 32 characters long.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenKeyIsMissing()
    {
        var settings = ValidSettings;
        settings.Key = null;

        var errors = settings.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("Jwt:Key is not configured.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenKeyIsWhitespaceOnly()
    {
        var settings = ValidSettings;
        settings.Key = new string(' ', 32);

        var errors = settings.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("Jwt:Key is not configured.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenIssuerIsWhitespaceOnly()
    {
        var settings = ValidSettings;
        settings.Issuer = "   ";

        var errors = settings.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("Jwt:Issuer is not configured.");
    }

    [Fact]
    public void Validate_ReturnsError_WhenAudienceIsWhitespaceOnly()
    {
        var settings = ValidSettings;
        settings.Audience = "   ";

        var errors = settings.Validate();

        errors.Should().ContainSingle().Which.Should().Contain("Jwt:Audience is not configured.");
    }
}
