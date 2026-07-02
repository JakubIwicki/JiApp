using api.JiApp.LovingBoards.Configuration;

namespace api.JiApp.LovingBoards.Tests.Configuration;

public sealed class LovingBoardsSettingsTests
{
    [Fact]
    public void Validate_WithNullJwt_ThrowsInvalidOperationException()
    {
        var settings = new LovingBoardsSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = null
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt*");
    }

    [Fact]
    public void Validate_WithNullConnectionString_ThrowsInvalidOperationException()
    {
        var settings = new LovingBoardsSettings
        {
            ConnectionString = null,
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionString*");
    }

    [Fact]
    public void Validate_WithMissingJwtKey_ThrowsInvalidOperationException()
    {
        var settings = new LovingBoardsSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = null, Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Key*");
    }

    [Fact]
    public void Validate_WithValidSettings_DoesNotThrow()
    {
        var settings = new LovingBoardsSettings
        {
            ConnectionString = "Data Source=test.db",
            Jwt = new JwtSettings { Key = "test-jwt-key-with-at-least-32-chars", Issuer = "iss", Audience = "aud" }
        };

        var act = () => settings.Validate();

        act.Should().NotThrow();
    }
}
