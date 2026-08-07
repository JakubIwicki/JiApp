using JiApp.Common.Authentication;
using JiApp.Gateway.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace JiApp.Gateway.Tests.Configuration;

public sealed class GatewaySettingsTests
{
    private static JwtSettings ValidJwt => new()
    {
        Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
        Issuer = "test-issuer",
        Audience = "test-audience"
    };

    private static GatewaySettings.RateLimitPolicyConfig ValidPolicy => new()
    {
        PermitLimit = 10,
        WindowInSeconds = 60,
        QueueLimit = 0,
        SegmentsPerWindow = 1
    };

    private static Dictionary<string, GatewaySettings.RateLimitPolicyConfig> ValidPolicies
    {
        get
        {
            var policies = new[]
            {
                "Login", "Register", "Refresh", "Logout", "Health", "DownloadFile",
                "SearchVideos", "SearchHistory", "DownloadHistory", "GetHistory",
                "Me", "GetDownloadLink", "DownloadStatus", "Preview", "Scheduler", "LovingBoards", "Assistant"
            };

            return policies.ToDictionary(p => p, _ => ValidPolicy);
        }
    }

    [Fact]
    public void Validate_Throws_WhenJwtIsNull()
    {
        var sut = new GatewaySettings();

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtKeyIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = string.Empty,
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtIssuerIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = string.Empty,
                Audience = "test-audience"
            },
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Issuer is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenJwtAudienceIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = string.Empty
            },
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenRateLimitingIsNull()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenRateLimitingIsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = "test-key-min-32-chars-!!!!!!!!!!!!!!!!",
                Issuer = "test-issuer",
                Audience = "test-audience"
            },
            RateLimiting = new Dictionary<string, GatewaySettings.RateLimitPolicyConfig>()
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting section is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenPolicyIsMissing()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = new Dictionary<string, GatewaySettings.RateLimitPolicyConfig>
            {
                ["Login"] = ValidPolicy,
                ["Register"] = ValidPolicy
            }
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RateLimiting:Refresh is not configured.*")
            .WithMessage("*RateLimiting:Logout is not configured.*")
            .WithMessage("*RateLimiting:Health is not configured.*")
            .WithMessage("*RateLimiting:DownloadFile is not configured.*")
            .WithMessage("*RateLimiting:SearchVideos is not configured.*")
            .WithMessage("*RateLimiting:SearchHistory is not configured.*")
            .WithMessage("*RateLimiting:DownloadHistory is not configured.*")
            .WithMessage("*RateLimiting:GetHistory is not configured.*")
            .WithMessage("*RateLimiting:Me is not configured.*")
            .WithMessage("*RateLimiting:GetDownloadLink is not configured.*")
            .WithMessage("*RateLimiting:DownloadStatus is not configured.*")
            .WithMessage("*RateLimiting:Preview is not configured.*")
            .WithMessage("*RateLimiting:Scheduler is not configured.*")
            .WithMessage("*RateLimiting:LovingBoards is not configured.*")
            .WithMessage("*RateLimiting:Assistant is not configured.*");
    }

    [Fact]
    public void Validate_Passes_WhenAllConfigured()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate(CreateEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_WhenMinVersionCodePositiveWithoutDownloadUrl()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            AppUpdate = new GatewaySettings.AppUpdateSettings
            {
                MinVersionCode = 66,
                DownloadUrl = string.Empty
            }
        };

        var act = () => sut.Validate(CreateEnvironment("Development"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AppUpdate:DownloadUrl is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenMinVersionCodeNegative()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            AppUpdate = new GatewaySettings.AppUpdateSettings
            {
                MinVersionCode = -1,
                DownloadUrl = "https://example.com/JiApp-latest.apk"
            }
        };

        var act = () => sut.Validate(CreateEnvironment("Development"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*AppUpdate:MinVersionCode must be greater than or equal to 0.*");
    }

    [Fact]
    public void Validate_Passes_WhenAppUpdateDormant()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            AppUpdate = new GatewaySettings.AppUpdateSettings
            {
                MinVersionCode = 0,
                DownloadUrl = string.Empty
            }
        };

        var act = () => sut.Validate(CreateEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_CollectsAllJwtErrors_Simultaneously()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = string.Empty,
                Issuer = string.Empty,
                Audience = string.Empty
            },
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*")
            .WithMessage("*Jwt:Issuer is not configured.*")
            .WithMessage("*Jwt:Audience is not configured.*");
    }

    [Fact]
    public void Validate_Throws_WhenProductionAndCorsAllowedOriginsMissing()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CorsAllowedOrigins is required in non-Development environments*");
    }

    [Fact]
    public void Validate_Throws_WhenProductionAndCorsAllowedOriginsEmpty()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            CorsAllowedOrigins = []
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*CorsAllowedOrigins is required in non-Development environments*");
    }

    [Fact]
    public void Validate_Throws_WhenProductionAndCorsOriginInvalid()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            CorsAllowedOrigins = ["not-a-url"]
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not-a-url*");
    }

    [Fact]
    public void Validate_Passes_WhenDevelopmentAndCorsAllowedOriginsMissing()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies
        };

        var act = () => sut.Validate(CreateEnvironment("Development"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Passes_WhenProductionAndCorsAllowedOriginsValid()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            CorsAllowedOrigins = ["https://app.example.com", "http://localhost:3000"]
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Passes_WhenProductionAndCorsAllowedOriginsWildcard()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            CorsAllowedOrigins = ["*"]
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_Throws_WhenProductionAndCorsOriginHasPath()
    {
        var sut = new GatewaySettings
        {
            Jwt = ValidJwt,
            RateLimiting = ValidPolicies,
            CorsAllowedOrigins = ["https://app.example.com/api"]
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not a valid http(s) origin*");
    }

    [Fact]
    public void Validate_CollectsJwtRateLimitingAndCorsErrors_Simultaneously()
    {
        var sut = new GatewaySettings
        {
            Jwt = new JwtSettings
            {
                Key = string.Empty,
                Issuer = "test-issuer",
                Audience = "test-audience"
            }
        };

        var act = () => sut.Validate(CreateEnvironment("Production"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Jwt:Key is not configured.*")
            .WithMessage("*RateLimiting section is not configured.*")
            .WithMessage("*CorsAllowedOrigins is required in non-Development environments*");
    }

    private static IWebHostEnvironment CreateEnvironment(string name)
    {
        var mock = new Mock<IWebHostEnvironment>();
        mock.SetupGet(e => e.EnvironmentName).Returns(name);
        return mock.Object;
    }
}
