using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiApp.Gateway.HealthDashboard;

public static class HealthDashboardEndpoint
{
    private const string Css = """
                               body{font-family:system-ui,sans-serif;max-width:700px;margin:40px auto;padding:0 20px;background:#111;color:#eee}
                               table{width:100%;border-collapse:collapse;margin-top:20px}
                               th,td{padding:12px 16px;text-align:left;border-bottom:1px solid #333}
                               th{background:#222}
                               h1{font-size:24px}
                               .footer{margin-top:30px;font-size:12px;color:#666}
                               """;

    private const string DashboardHtmlTemplate = """
                                                 <!DOCTYPE html>
                                                 <html>
                                                 <head><title>JiApp Health Dashboard</title>
                                                 <meta charset="utf-8"><meta http-equiv="refresh" content="30">
                                                 <style>{0}</style></head>
                                                 <body>
                                                 <h1>JiApp Health Dashboard</h1>
                                                 <table><thead><tr><th>Service</th><th>Endpoint</th><th>Status</th></tr></thead>
                                                 <tbody>{1}</tbody></table>
                                                 <p class="footer">Auto-refresh every 30s · JiApp Gateway</p>
                                                 </body></html>
                                                 """;

    public static void MapHealthDashboard(this IEndpointRouteBuilder endpoints,
        string identityUrl, string ytUrl, string? imageToolsUrl = null, string? schedulerUrl = null, string? lovingBoardsUrl = null)
    {
        endpoints.MapGet("/health/dashboard", async (HttpContext context) =>
        {
            var html = await BuildDashboardHtml(
                context.RequestServices, identityUrl, ytUrl, imageToolsUrl, schedulerUrl, lovingBoardsUrl,
                context.RequestAborted);
            context.Response.ContentType = "text/html; charset=utf-8";
            await context.Response.WriteAsync(html, context.RequestAborted);
        });
    }

    private static async Task<string> BuildDashboardHtml(
        IServiceProvider sp, string identityUrl, string ytUrl, string? imageToolsUrl, string? schedulerUrl,
        string? lovingBoardsUrl, CancellationToken ct)
    {
        var http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("healthCheck");
        http.Timeout = TimeSpan.FromSeconds(5);
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("JiApp.Gateway.HealthDashboard");

        var tasks = new List<Task<HealthCheckResult>>
        {
            CheckService(http, logger, "Identity", $"{identityUrl}/api/v1/auth/health", ct),
            CheckService(http, logger, "YT Downloader", $"{ytUrl}/api/v1/yt/health", ct),
        };

        if (imageToolsUrl is not null)
            tasks.Add(CheckService(http, logger, "Image Tools", $"{imageToolsUrl}/api/v1/imagetools/health", ct));

        if (schedulerUrl is not null)
            tasks.Add(CheckService(http, logger, "Scheduler", $"{schedulerUrl}/api/v1/scheduler/health", ct));

        if (lovingBoardsUrl is not null)
            tasks.Add(CheckService(http, logger, "LovingBoards", $"{lovingBoardsUrl}/api/v1/lovingboards/health", ct));

        var results = await Task.WhenAll(tasks);

        var rows = string.Join("\n", results.Select(r =>
            $"<tr><td>{r.Name}</td><td>{r.Url}</td>" +
            $"<td style='color:{r.Color};font-weight:bold'>{r.Status}</td></tr>"));

        return string.Format(DashboardHtmlTemplate, Css, rows);
    }

    private static async Task<HealthCheckResult> CheckService(
        HttpClient http, ILogger logger, string name, string url, CancellationToken ct)
    {
        try
        {
            var response = await http.GetAsync(url, ct);
            var healthy = response.IsSuccessStatusCode;
            return new HealthCheckResult(name, url,
                healthy ? "HEALTHY" : $"DOWN ({(int)response.StatusCode})",
                healthy ? "#4caf50" : "#f44336");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed for {ServiceName} at {Url}", name, url);
            return new HealthCheckResult(name, url, "UNREACHABLE", "#f44336");
        }
    }
}