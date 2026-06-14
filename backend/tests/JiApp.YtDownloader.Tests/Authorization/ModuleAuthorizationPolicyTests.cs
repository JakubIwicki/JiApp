using System.Security.Claims;
using JiApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Authorization;

public class ModuleAuthorizationPolicyTests
{
    private const string PolicyName = "module:YtDownloader";

    private sealed class Fixture
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly AuthorizationPolicy _policy;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyName, policy =>
                    policy.RequireClaim("module", Modules.YtDownloader));

            var provider = services.BuildServiceProvider();
            _authorizationService = provider.GetRequiredService<IAuthorizationService>();

            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
            _policy = policyProvider.GetPolicyAsync(PolicyName).GetAwaiter().GetResult()!;
        }

        public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user)
        {
            var result = await _authorizationService.AuthorizeAsync(user, resource: null, _policy);
            return result.Succeeded;
        }

        public static ClaimsPrincipal AuthenticatedUserWithClaims(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
            return new ClaimsPrincipal(identity);
        }
    }

    [Fact]
    public async Task Policy_denies_authenticated_user_without_module_claim()
    {
        // Arrange
        var fixture = new Fixture();
        var user = Fixture.AuthenticatedUserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"));

        // Act
        var authorized = await fixture.IsAuthorizedAsync(user);

        // Assert — a valid token lacking the module claim yields 403
        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_denies_user_holding_only_other_module_claim()
    {
        // Arrange
        var fixture = new Fixture();
        var user = Fixture.AuthenticatedUserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("module", Modules.Scheduler));

        // Act
        var authorized = await fixture.IsAuthorizedAsync(user);

        // Assert
        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_allows_user_holding_ytdownloader_module_claim()
    {
        // Arrange
        var fixture = new Fixture();
        var user = Fixture.AuthenticatedUserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("module", Modules.YtDownloader));

        // Act
        var authorized = await fixture.IsAuthorizedAsync(user);

        // Assert
        authorized.Should().BeTrue();
    }
}
