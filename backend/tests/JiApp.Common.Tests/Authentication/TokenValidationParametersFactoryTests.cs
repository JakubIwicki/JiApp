using JiApp.Common.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Common.Tests.Authentication;

public sealed class TokenValidationParametersFactoryTests
{
    [Fact]
    public void Create_SetsClockSkewToZero()
    {
        var parameters = TokenValidationParametersFactory.Create("test-key", "test-issuer", "test-audience");

        parameters.ClockSkew.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void Create_RestrictsValidAlgorithmsToHS256()
    {
        var parameters = TokenValidationParametersFactory.Create("test-key", "test-issuer", "test-audience");

        parameters.ValidAlgorithms.Should().ContainSingle().Which.Should().Be("HS256");
    }

    [Fact]
    public void Create_SetsIssuerAudienceAndSigningKey_FromArguments()
    {
        var parameters = TokenValidationParametersFactory.Create("test-key-at-least-32-characters!", "test-issuer", "test-audience");

        parameters.ValidIssuer.Should().Be("test-issuer");
        parameters.ValidAudience.Should().Be("test-audience");
        parameters.IssuerSigningKey.Should().BeOfType<SymmetricSecurityKey>()
            .Which.Key.Should().BeEquivalentTo(System.Text.Encoding.UTF8.GetBytes("test-key-at-least-32-characters!"));
    }
}
