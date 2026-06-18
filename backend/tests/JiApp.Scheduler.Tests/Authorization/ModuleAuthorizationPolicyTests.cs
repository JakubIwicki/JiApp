using System.Security.Claims;
using JiApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Scheduler.Tests.Authorization;

public sealed class ModuleAuthorizationPolicyTests
{
    private const string PolicyName = "module:Scheduler";

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
                    policy.RequireClaim("module", Modules.Scheduler));

            var provider = services.BuildServiceProvider();
            _authorizationService = provider.GetRequiredService<IAuthorizationService>();

            var policyProvider = provider.GetRequiredService<IAuthorizationPolicyProvider>();
            _policy = policyProvider.GetPolicyAsync(PolicyName).GetAwaiter().GetResult()!;
        }

        public static Fixture Init() => new();

        public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user)
        {
            var result = await _authorizationService.AuthorizeAsync(user, resource: null, _policy);
            return result.Succeeded;
        }

        public static ClaimsPrincipal UserWithClaims(params Claim[] claims)
        {
            var identity = new ClaimsIdentity(claims, authenticationType: "TestAuth");
            return new ClaimsPrincipal(identity);
        }
    }

    [Fact]
    public async Task Policy_DeniesAuthenticatedUser_WithoutModuleClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_DeniesUser_HoldingOnlyOtherModuleClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("module", Modules.YtDownloader));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_AllowsUser_HoldingSchedulerModuleClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("module", Modules.Scheduler));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeTrue();
    }
}
