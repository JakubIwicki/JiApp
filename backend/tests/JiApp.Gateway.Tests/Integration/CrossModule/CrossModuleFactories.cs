using api.JiApp.LovingBoards.Persistence;
using JiApp.Identity.Persistence;
using JiApp.Scheduler.Persistence;
using JiApp.YtApi.Clients;
using JiApp.YtApi.Contracts;
using JiApp.YtDownloader.Persistence;
using System.Net;
using JiApp.Testing.Common.Auth;
using JiApp.Testing.Common.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog;

namespace JiApp.Gateway.Tests.Integration.CrossModule;

/// <summary>
/// Hosts for the cross-module Gateway E2E tier. The three module hosts listen on
/// real ephemeral Kestrel sockets so the Scheduler's RemoteSecurityStampValidator
/// and the LovingBoards UserExistenceClient make genuine HTTP round trips to the
/// Identity host; the Gateway stays on the in-memory TestServer and proxies to those
/// sockets.
/// <para>
/// The module hosts are built manually with <c>WebApplication.CreateBuilder()</c> +
/// <c>UseKestrel</c> because <c>WebApplicationFactory</c> always wires the in-memory
/// TestServer, which cannot accept the real cross-host TCP calls this tier requires.
/// They share the exact <c>Startup</c> class from each src host and use one long-lived
/// in-memory SQLite connection shared by every request scope (the store-swap pattern).
/// Config is applied via in-memory configuration since the referenced hosts' appsettings
/// content is suppressed in Gateway.Tests; all hosts share <see cref="TestTokens.JwtKey"/>
/// so identity-issued and minted tokens validate everywhere.
/// </para>
/// </summary>
public abstract class RealModuleHost<TDbContext> : IDisposable
    where TDbContext : DbContext
{
    private readonly SqliteConnection _connection;
    private readonly WebApplication _app;

    protected RealModuleHost()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _app = Build(_connection);
        PostBuild(_app);
        _app.StartAsync().GetAwaiter().GetResult();
        BaseAddress = new Uri(_app.Urls.First(u => u.StartsWith("http://127.0.0.1:")) + "/");
    }

    /// <summary>The real base address (http://127.0.0.1:&lt;ephemeral-port&gt;).</summary>
    public Uri BaseAddress { get; }

    /// <summary>Reads persisted state through a fresh request scope on the shared connection.</summary>
    public T InFreshScope<T>(Func<TDbContext, T> read)
    {
        using var scope = _app.Services.CreateScope();
        return read(scope.ServiceProvider.GetRequiredService<TDbContext>());
    }

    public void Dispose()
    {
        _app.StopAsync().GetAwaiter().GetResult();
        _app.DisposeAsync().GetAwaiter().GetResult();
        _connection.Dispose();
    }

    protected abstract WebApplication Build(SqliteConnection connection);

    protected virtual void PostBuild(WebApplication app)
    {
    }

    protected static WebApplicationBuilder CreateBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = "Test";
        // Matches each module host's Program.Main: registers the Serilog
        // DiagnosticContext that UseSerilogRequestLogging() resolves.
        builder.Host.UseSerilog((context, config) =>
            config.ReadFrom.Configuration(context.Configuration));
        // Port 0 = ephemeral. ListenLocalhost(0) rejects dynamic ports; bind the
        // loopback address explicitly so Kestrel picks a free port.
        builder.WebHost.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
        // Drop the default disk config sources (appsettings*.json + env vars). A module's
        // appsettings.json carries a Kestrel HTTPS endpoint pointing at a dev-cert PFX that
        // does not exist in the test output — its config-based binding would crash Kestrel at
        // boot. Each factory supplies the host's entire config via the in-memory collection it
        // adds after CreateBuilder returns.
        builder.Configuration.Sources.Clear();
        return builder;
    }

    protected static void SwapStore<T>(IServiceCollection services, SqliteConnection connection) where T : DbContext
    {
        services.RemoveAll<DbContextOptions<T>>();
        services.RemoveAll<T>();
        services.AddDbContext<T>(options => options.UseSqlite(connection));
    }
}

public sealed class CrossModuleIdentityWebApplicationFactory : RealModuleHost<IdentityDbContext>
{
    protected override WebApplication Build(SqliteConnection connection)
    {
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionString"] = "Data Source=:memory:",
            ["CorsAllowedOrigins:0"] = "*",
            ["Jwt:Key"] = TestTokens.JwtKey,
            ["Jwt:Issuer"] = TestTokens.JwtIssuer,
            ["Jwt:Audience"] = TestTokens.JwtAudience,
            ["Jwt:AccessTokenExpireMinutes"] = "60",
            ["Jwt:RefreshTokenExpireDays"] = "7",
            ["RateLimiting:Login:PermitLimit"] = "100",
            ["RateLimiting:Login:WindowInSeconds"] = "60",
            ["RateLimiting:Login:QueueLimit"] = "0",
            ["RateLimiting:Login:SegmentsPerWindow"] = "0",
            ["RateLimiting:Register:PermitLimit"] = "100",
            ["RateLimiting:Register:WindowInSeconds"] = "60",
            ["RateLimiting:Register:QueueLimit"] = "0",
            ["RateLimiting:Register:SegmentsPerWindow"] = "0",
            ["RateLimiting:Refresh:PermitLimit"] = "100",
            ["RateLimiting:Refresh:WindowInSeconds"] = "60",
            ["RateLimiting:Refresh:QueueLimit"] = "0",
            ["RateLimiting:Refresh:SegmentsPerWindow"] = "0",
            ["RateLimiting:Logout:PermitLimit"] = "100",
            ["RateLimiting:Logout:WindowInSeconds"] = "60",
            ["RateLimiting:Logout:QueueLimit"] = "0",
            ["RateLimiting:Logout:SegmentsPerWindow"] = "0",
        });

        var settings = new JiApp.Identity.Configuration.IdentitySettings();
        builder.Configuration.Bind(settings);
        settings.Validate(builder.Environment);
        var startup = new JiApp.Identity.Startup(settings, builder.Environment);
        startup.ConfigureServices(builder.Services);
        SwapStore<IdentityDbContext>(builder.Services, connection);
        RateLimitPolicyOverrides.ApplyBudget(builder.Services, loginPermitLimit: 100, registerPermitLimit: 100);
        var app = builder.Build();
        JiApp.Identity.Startup.Configure(app);
        return app;
    }

    protected override void PostBuild(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IdentityDbContext>().Database.Migrate();
        scope.ServiceProvider.GetRequiredService<JiApp.Identity.Services.IRoleSeeder>()
            .SeedAsync().GetAwaiter().GetResult();
    }
}

public sealed class CrossModuleSchedulerWebApplicationFactory(string identityBaseUrl) : RealModuleHost<SchedulerDbContext>
{
    protected override WebApplication Build(SqliteConnection connection)
    {
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionString"] = "Data Source=:memory:",
            ["CorsAllowedOrigins:0"] = "*",
            ["Jwt:Key"] = TestTokens.JwtKey,
            ["Jwt:Issuer"] = TestTokens.JwtIssuer,
            ["Jwt:Audience"] = TestTokens.JwtAudience,
            ["IdentityBaseUrl"] = identityBaseUrl,
        });

        var settings = new JiApp.Scheduler.Configuration.SchedulerSettings();
        builder.Configuration.Bind(settings);
        settings.Validate(builder.Environment);
        var startup = new JiApp.Scheduler.Startup(settings, builder.Environment);
        startup.ConfigureServices(builder.Services);
        SwapStore<SchedulerDbContext>(builder.Services, connection);
        var app = builder.Build();
        JiApp.Scheduler.Startup.Configure(app);
        return app;
    }

    protected override void PostBuild(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<SchedulerDbContext>().Database.Migrate();
    }
}

public sealed class CrossModuleLovingBoardsWebApplicationFactory(string identityBaseUrl)
    : RealModuleHost<LovingBoardsDbContext>
{
    protected override WebApplication Build(SqliteConnection connection)
    {
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionString"] = "Data Source=:memory:",
            ["CorsAllowedOrigins:0"] = "*",
            ["Jwt:Key"] = TestTokens.JwtKey,
            ["Jwt:Issuer"] = TestTokens.JwtIssuer,
            ["Jwt:Audience"] = TestTokens.JwtAudience,
            ["IdentityBaseUrl"] = identityBaseUrl,
        });

        var settings = new api.JiApp.LovingBoards.Configuration.LovingBoardsSettings();
        builder.Configuration.Bind(settings);
        settings.Validate(builder.Environment);
        var startup = new api.JiApp.LovingBoards.Startup(settings, builder.Environment);
        startup.ConfigureServices(builder.Services);
        SwapStore<LovingBoardsDbContext>(builder.Services, connection);
        var app = builder.Build();
        api.JiApp.LovingBoards.Startup.Configure(app);
        return app;
    }

    protected override void PostBuild(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<LovingBoardsDbContext>().Database.Migrate();
    }
}

public sealed class CrossModuleYtDownloaderWebApplicationFactory : RealModuleHost<YtDbContext>
{
    /// <summary>
    /// The public base URL the module must use for download links. The Tier B journey
    /// asserts the proxied download response is built from this value — a regression to
    /// the request host (PR #151/#152: the internal docker hostname leaking into links)
    /// fails the test.
    /// </summary>
    public const string TestPublicBaseUrl = "https://downloads.example.com";

    private static readonly IYoutubeClient YoutubeClient = CreateYoutubeClientStub();

    protected override WebApplication Build(SqliteConnection connection)
    {
        var builder = CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionString"] = "Data Source=:memory:",
            ["CorsAllowedOrigins:0"] = "*",
            ["Jwt:Key"] = TestTokens.JwtKey,
            ["Jwt:Issuer"] = TestTokens.JwtIssuer,
            ["Jwt:Audience"] = TestTokens.JwtAudience,
            ["App:BaseDirectory"] = Path.GetTempPath(),
            ["App:PreviewDurationSeconds"] = "10",
            ["App:DownloadTtlMinutes"] = "15",
            ["App:DownloadJobTimeoutMinutes"] = "5",
            ["App:PublicBaseUrl"] = TestPublicBaseUrl,
            ["Youtube:ApiKey"] = "test-api-key",
            ["Youtube:YtDlpPath"] = "/usr/bin/true",
            ["Youtube:FfmpegPath"] = "/usr/bin/true",
            ["Youtube:MaxResults"] = "30",
            ["Youtube:PageSize"] = "10",
        });

        var settings = new JiApp.YtDownloader.Configuration.Settings();
        builder.Configuration.Bind(settings);
        settings.Validate(builder.Environment);
        var startup = new JiApp.YtDownloader.Startup(settings, builder.Environment);
        startup.ConfigureServices(builder.Services);
        SwapStore<YtDbContext>(builder.Services, connection);

        // The real host would start DownloadWorker and TempFileCleanupService (which shell
        // out to yt-dlp) plus the real YoutubeClient (Google API). Remove all hosted
        // services and stub IYoutubeClient so no external process or API call leaves the
        // process — the same doubling the YtDownloaderPipelineWebApplicationFactory uses.
        builder.Services.RemoveAll<IHostedService>();
        builder.Services.RemoveAll<IYoutubeClient>();
        builder.Services.AddSingleton(YoutubeClient);

        var app = builder.Build();
        JiApp.YtDownloader.Startup.Configure(app);
        return app;
    }

    protected override void PostBuild(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<YtDbContext>().Database.Migrate();
    }

    private static IYoutubeClient CreateYoutubeClientStub()
    {
        var mock = new Mock<IYoutubeClient>();

        mock.Setup(c => c.SearchVideosAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<YoutubeVideo>
            {
                new("dQw4w9WgXcQ", "Test Video", "Test Description",
                    "https://img.youtube.com/vi/dQw4w9WgXcQ/default.jpg", "Test Channel")
            });
        mock.Setup(c => c.GetVideoByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not expected in these tests"));
        mock.Setup(c => c.DownloadVideoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("not expected in these tests"));
        mock.Setup(c => c.BuildPreviewAudioProcess(It.IsAny<string>()))
            .Throws(new InvalidOperationException("not expected in these tests"));

        return mock.Object;
    }
}

public sealed class CrossModuleGatewayWebApplicationFactory(
    string identityBaseUrl, string schedulerUrl, string lovingBoardsUrl, string ytDownloaderUrl)
    : IntegrationTestBase<JiApp.Gateway.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        ApplyJwtSettings(builder);
        builder.UseSetting("CorsAllowedOrigins:0", "*");
        ApplyGatewayRateLimiting(builder);
        ApplyReverseProxy(builder, identityBaseUrl, schedulerUrl, lovingBoardsUrl, ytDownloaderUrl);

        // High budgets so the register/login/me/scheduler/lovingboards calls in the
        // suite can never trip the Gateway's own rate limiter.
        builder.UseSetting("RateLimiting:Login:PermitLimit", "200");
        builder.UseSetting("RateLimiting:Register:PermitLimit", "200");
        builder.UseSetting("RateLimiting:Me:PermitLimit", "200");
        builder.UseSetting("RateLimiting:Scheduler:PermitLimit", "200");
        builder.UseSetting("RateLimiting:LovingBoards:PermitLimit", "200");
    }

    private static void ApplyJwtSettings(IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Key", TestTokens.JwtKey);
        builder.UseSetting("Jwt:Issuer", TestTokens.JwtIssuer);
        builder.UseSetting("Jwt:Audience", TestTokens.JwtAudience);
    }

    /// <summary>
    /// The Gateway's rate-limit policies (mirrors its base appsettings.json). Every
    /// policy the <see cref="JiApp.Gateway.RateLimiting.RateLimitPolicySelector"/> can
    /// attach must be registered here, else the Gateway fails closed with a 500.
    /// </summary>
    private static void ApplyGatewayRateLimiting(IWebHostBuilder builder)
    {
        ApplyRateLimits(builder,
            ("Login", 5, 60, 0, 0),
            ("Register", 3, 60, 0, 0),
            ("Refresh", 10, 60, 0, 0),
            ("Logout", 10, 60, 0, 0),
            ("Health", 30, 60, 0, 0),
            ("DownloadFile", 10, 60, 0, 4),
            ("SearchVideos", 30, 60, 0, 4),
            ("SearchHistory", 20, 60, 0, 0),
            ("DownloadHistory", 20, 60, 0, 0),
            ("GetHistory", 20, 60, 0, 0),
            ("Me", 60, 60, 0, 0),
            ("GetDownloadLink", 10, 60, 0, 4),
            ("DownloadStatus", 120, 60, 0, 0),
            ("Preview", 30, 60, 0, 4),
            ("Scheduler", 30, 60, 0, 4),
            ("LovingBoards", 30, 60, 0, 4),
            ("Assistant", 6, 60, 0, 4));
    }

    private static void ApplyRateLimits(IWebHostBuilder builder, params (string Name, int Permit, int Window, int Queue, int Segments)[] policies)
    {
        foreach (var (name, permit, window, queue, segments) in policies)
        {
            builder.UseSetting($"RateLimiting:{name}:PermitLimit", permit.ToString());
            builder.UseSetting($"RateLimiting:{name}:WindowInSeconds", window.ToString());
            builder.UseSetting($"RateLimiting:{name}:QueueLimit", queue.ToString());
            builder.UseSetting($"RateLimiting:{name}:SegmentsPerWindow", segments.ToString());
        }
    }

    private static void ApplyReverseProxy(IWebHostBuilder builder, string identityUrl, string schedulerUrl, string lovingBoardsUrl, string ytDownloaderUrl)
    {
        // Routes — mirror the Gateway's base appsettings.json so proxying works even
        // when the appsettings content is suppressed in this process.
        builder.UseSetting("ReverseProxy:Routes:identity-route:ClusterId", "identity-cluster");
        builder.UseSetting("ReverseProxy:Routes:identity-route:Match:Path", "/api/v1/auth/{**catch-all}");
        builder.UseSetting("ReverseProxy:Routes:scheduler-route:ClusterId", "scheduler-cluster");
        builder.UseSetting("ReverseProxy:Routes:scheduler-route:Match:Path", "/api/v1/scheduler/{**catch-all}");
        builder.UseSetting("ReverseProxy:Routes:lovingboards-route:ClusterId", "lovingboards-cluster");
        builder.UseSetting("ReverseProxy:Routes:lovingboards-route:Match:Path", "/api/v1/lovingboards/{**catch-all}");
        builder.UseSetting("ReverseProxy:Routes:yt-route:ClusterId", "yt-cluster");
        builder.UseSetting("ReverseProxy:Routes:yt-route:Match:Path", "/api/v1/yt/{**catch-all}");

        // Cluster destinations — point YARP at the real ephemeral Kestrel sockets.
        builder.UseSetting("ReverseProxy:Clusters:identity-cluster:Destinations:identity:Address", identityUrl);
        builder.UseSetting("ReverseProxy:Clusters:scheduler-cluster:Destinations:scheduler:Address", schedulerUrl);
        builder.UseSetting("ReverseProxy:Clusters:lovingboards-cluster:Destinations:lovingboards:Address", lovingBoardsUrl);
        builder.UseSetting("ReverseProxy:Clusters:yt-cluster:Destinations:yt:Address", ytDownloaderUrl);
    }
}
