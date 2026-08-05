using System.Threading.RateLimiting;
using JiApp.Identity.Configuration;
using JiApp.Identity.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace JiApp.Identity.Tests.Integration;

/// <summary>
/// Test-only override for the rate-limit budgets.
///
/// The ConfigureAppConfiguration in-memory override requested for the factories
/// does NOT take effect here: in this minimal-hosting WebApplicationFactory the
/// app's appsettings providers are added after the factory's callback, and the
/// IdentitySettings bind in Program runs at CreateBuilder time — so the bound
/// budgets stay at the appsettings.json values. The effective lever is the
/// options service: Startup.AddRateLimiter registers the limiter options via
/// the OPEN generic IOptions&lt;RateLimiterOptions&gt;, so registering a CLOSED
/// IOptions&lt;RateLimiterOptions&gt; that returns a pre-built options object
/// wins DI resolution and bypasses the OptionsFactory (where re-adding the same
/// policy names would throw). Window/queue shape mirrors the base config
/// (fixed window, 60s, queue 0).
/// </summary>
internal static class RateLimitPolicyOverrides
{
    // Refresh/Logout are not exercised by these suites; a high budget keeps them
    // out of the way while still registering a policy for every RateLimitPolicyNames
    // value so a future fact hitting /refresh or /logout never 500s.
    private const int UnusedPolicyPermitLimit = 100;

    public static void ApplyBudget(IServiceCollection services, int loginPermitLimit, int registerPermitLimit)
    {
        services.RemoveAll<IOptions<RateLimiterOptions>>();
        services.AddSingleton<IOptions<RateLimiterOptions>>(_ =>
            Options.Create(BuildOptions(loginPermitLimit, registerPermitLimit)));
    }

    private static RateLimiterOptions BuildOptions(int loginPermitLimit, int registerPermitLimit)
    {
        var options = new RateLimiterOptions
        {
            RejectionStatusCode = StatusCodes.Status429TooManyRequests,
        };
        options.AddPolicy(RateLimitPolicyNames.LoginPolicy, CreateFixedWindowPartition(loginPermitLimit));
        options.AddPolicy(RateLimitPolicyNames.RegisterPolicy, CreateFixedWindowPartition(registerPermitLimit));
        options.AddPolicy(RateLimitPolicyNames.RefreshPolicy, CreateFixedWindowPartition(UnusedPolicyPermitLimit));
        options.AddPolicy(RateLimitPolicyNames.LogoutPolicy, CreateFixedWindowPartition(UnusedPolicyPermitLimit));

        return options;
    }

    private static Func<HttpContext, RateLimitPartition<string>> CreateFixedWindowPartition(int permitLimit) =>
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            RateLimitPartitioning.GetPartitionKey(httpContext),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromSeconds(60),
                QueueLimit = 0,
            });
}
