using System.Security.Claims;
using JiApp.Common;
using JiApp.Common.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Scheduler.Tests.Authorization;

public sealed class PermissionAuthorizationPolicyTests
{
    private const string PolicyName = Permissions.SchedulerAccess;

    private sealed class Fixture
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly AuthorizationPolicy _policy;

        public Fixture()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
            services.AddAuthorizationBuilder()
                .AddPolicy(PolicyName, policy =>
                    policy.RequirePermission(Permissions.SchedulerAccess));

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
    public async Task Policy_DeniesAuthenticatedUser_WithoutPermissionClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_DeniesUser_HoldingOnlyOtherPermissionClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("permission", Permissions.YtDownloaderAccess));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeFalse();
    }

    [Fact]
    public async Task Policy_AllowsUser_HoldingSchedulerAccessPermission()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim("permission", Permissions.SchedulerAccess));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeTrue();
    }

    [Fact]
    public async Task Policy_AllowsAdminUser_WithoutPermissionClaim()
    {
        var fixture = Fixture.Init();
        var user = Fixture.UserWithClaims(
            new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Role, RoleNames.Admin));

        var authorized = await fixture.IsAuthorizedAsync(user);

        authorized.Should().BeTrue();
    }
}
