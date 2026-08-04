using System.Net;
using IPNetwork = System.Net.IPNetwork;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiApp.Common.Middleware;

public static class ForwardedHeadersExtensions
{
    /// <summary>
    /// Trusts X-Forwarded-* headers only from proxies in ForwardedHeaders:KnownNetworks (CIDRs)
    /// / ForwardedHeaders:KnownProxies (IPs). With zero configured entries nothing is trusted and
    /// the middleware is not registered, so an unconfigured deploy is a true no-op.
    /// </summary>
    public static IApplicationBuilder UseTrustedForwardedHeaders(
        this IApplicationBuilder app, IConfiguration configuration)
    {
        var knownNetworks = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>();
        var knownProxies = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>();

        if (knownNetworks is not { Length: > 0 } && knownProxies is not { Length: > 0 })
        {
            if (configuration.GetSection("ForwardedHeaders").Exists())
                LogTrustedForwardingDisabled(app, knownNetworks, knownProxies);
            return app;
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
        };

        if (knownNetworks is { Length: > 0 })
        {
            foreach (var entry in knownNetworks)
            {
                if (IPNetwork.TryParse(entry, out var network))
                    options.KnownIPNetworks.Add(network);
            }
        }

        if (knownProxies is { Length: > 0 })
        {
            foreach (var entry in knownProxies)
            {
                if (IPAddress.TryParse(entry, out var address))
                    options.KnownProxies.Add(address);
            }
        }

        if (options.KnownIPNetworks.Count == 0 && options.KnownProxies.Count == 0)
        {
            LogTrustedForwardingDisabled(app, knownNetworks, knownProxies);
            return app;
        }

        app.UseForwardedHeaders(options);
        return app;
    }

    private static void LogTrustedForwardingDisabled(
        IApplicationBuilder app, string[]? knownNetworks, string[]? knownProxies)
    {
        var logger = app.ApplicationServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("JiApp.Common.Middleware.ForwardedHeadersExtensions");
        logger.LogWarning(
            "ForwardedHeaders section configured but produced no KnownNetworks/KnownProxies — trusted-forwarding is DISABLED. Use indexed env keys (ForwardedHeaders__KnownNetworks__0) with CIDR entries. Raw configured values — KnownNetworks: {KnownNetworks}, KnownProxies: {KnownProxies}",
            FormatEntries(knownNetworks),
            FormatEntries(knownProxies));
    }

    private static string FormatEntries(string[]? entries) =>
        entries is { Length: > 0 }
            ? string.Join(", ", entries)
            : "<not bound — configure with indexed env keys e.g. ForwardedHeaders__KnownNetworks__0>";
}
