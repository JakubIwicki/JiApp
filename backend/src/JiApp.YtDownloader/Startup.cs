using System.Text.Json;
using System.Threading.Channels;
using FluentValidation;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Authentication;
using JiApp.Common.Authorization;
using JiApp.Common.Constants;
using JiApp.Common.Middleware;
using JiApp.Common.Resilience;
using Microsoft.AspNetCore.Authorization;
using JiApp.Common.Services;
using JiApp.YtApi.Clients;
using JiApp.YtDownloader.Agent;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.ArchiveDownload;
using JiApp.YtDownloader.Features.ArchiveSearch;
using JiApp.YtDownloader.Features.Assistant;
using JiApp.YtDownloader.Features.DownloadFile;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.DownloadStatus;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Features.GetHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Features.StreamPreview;
using JiApp.YtDownloader.Mcp;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;

namespace JiApp.YtDownloader;

public class Startup(Settings settings, IWebHostEnvironment env)
{
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureInfrastructure(services);
        ConfigureOpenApi(services);
        ConfigurePersistence(services, settings);
        ConfigureAuth(services, settings);
        ConfigureCors(services, settings, env);
        ConfigureApplicationServices(services, settings);
        ConfigureFeatureHandlers(services);
        ConfigureBackgroundServices(services);
    }

    private static void ConfigureInfrastructure(IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IRetryPolicyFactory, RetryPolicyFactory>();
        services.AddTransient<GlobalExceptionMiddleware>();
        services.AddHttpContextAccessor();
    }

    private static void ConfigureOpenApi(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
    }

    private static void ConfigurePersistence(IServiceCollection services, Settings settings)
    {
        services.AddDbContext<YtDbContext>(options =>
        {
            if (settings.ConnectionString!.Contains("Host="))
                options.UseNpgsql(settings.ConnectionString);
            else
                options.UseSqlite(settings.ConnectionString)
                    .AddInterceptors(new SqliteBusyTimeoutInterceptor());
        });
    }

    private static void ConfigureAuth(IServiceCollection services, Settings settings)
    {
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = TokenValidationParametersFactory.Create(
                    settings.Jwt!.Key!, settings.Jwt!.Issuer!, settings.Jwt!.Audience!);

                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var response = JsonSerializer.Serialize(
                            new ApiErrorResponse(Error: "Unauthorized"), ApiErrorResponse.JsonOptions);
                        return context.Response.WriteAsync(response);
                    }
                };
            });

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(Permissions.YtDownloaderAccess, policy =>
                policy.RequirePermission(Permissions.YtDownloaderAccess));
    }

    private static void ConfigureCors(IServiceCollection services, Settings settings, IWebHostEnvironment env)
    {
        // CORS — AllowCredentials prevents using AllowAnyOrigin, so we use
        // SetIsOriginAllowed with explicit origin lists. In Development, accept
        // any origin when no origins are configured. In all other environments,
        // fail closed if CorsAllowedOrigins is missing.
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();

                if (settings.CorsAllowedOrigins is { Length: > 0 } origins)
                    policy.SetIsOriginAllowed(origin => origins.Contains(origin));
                else if (env.IsDevelopment())
                    policy.SetIsOriginAllowed(_ => true);
                else
                    throw new InvalidOperationException("CorsAllowedOrigins must be configured in non-Development environments.");
            });
        });
    }

    private static void ConfigureApplicationServices(IServiceCollection services, Settings settings)
    {
        // Repositories
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
        services.AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>();
        services.AddScoped<IAssistantUsageRepository, AssistantUsageRepository>();
        // Services
        services.AddSingleton(sp => new DownloadJobStore(
            sp.GetRequiredService<IServiceScopeFactory>(),
            TimeSpan.FromMinutes(settings.App?.DownloadTtlMinutes ?? 15),
            sp.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IDownloadJobStore>(sp => sp.GetRequiredService<DownloadJobStore>());
        services.AddSingleton<IDownloadQueue>(sp => sp.GetRequiredService<DownloadJobStore>());
        services.AddSingleton(_ => Channel.CreateUnbounded<string>());
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IYoutubeClient>(sp =>
            new YoutubeClient(
                settings.Youtube!.ApiKey!,
                settings.Youtube!.YtDlpPath!,
                settings.Youtube!.FfmpegPath!,
                settings.Youtube!.CookiesFile,
                settings.Youtube!.CookiesFromBrowser,
                settings.Youtube!.Proxy,
                // NumTries = 1 disables Google.Apis' internal retry loop so the owned Polly
                // policy below is the sole retry owner — retry budgets never stack upstream.
                httpClientFactory: new SingleTryGoogleHttpClientFactory(),
                retryPolicyFactory: sp.GetRequiredService<IRetryPolicyFactory>()));

        services.AddSingleton(settings);
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024;
            options.CompactionPercentage = 0.25;
        });
    }

    private static void ConfigureFeatureHandlers(IServiceCollection services)
    {
        // Validators
        services.AddScoped<IValidator<SearchVideosRequest>, SearchVideosValidator>();
        services.AddScoped<IValidator<SearchHistoryRequest>, SearchHistoryValidator>();
        services.AddScoped<IValidator<ArchiveSearchRequest>, ArchiveSearchValidator>();
        services.AddScoped<IValidator<DownloadRequest>, GetDownloadLinkValidator>();
        services.AddScoped<IValidator<DownloadHistoryRequest>, DownloadHistoryValidator>();
        services.AddScoped<IValidator<ArchiveDownloadRequest>, ArchiveDownloadValidator>();
        services.AddScoped<IValidator<GetHistoryRequest>, GetHistoryValidator>();
        services.AddScoped<IValidator<AssistantChatRequest>, AssistantChatValidator>();

        // Handlers
        services.AddScoped<SearchVideosHandler>();
        services.AddScoped<SearchHistoryHandler>();
        services.AddScoped<ArchiveSearchHandler>();
        services.AddScoped<GetDownloadLinkHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<DownloadHistoryHandler>();
        services.AddScoped<DownloadStatusHandler>();
        services.AddScoped<ArchiveDownloadHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddScoped<StreamPreviewHandler>();
        services.AddScoped<YtAgentToolService>();

        // Assistant chat (DeepSeek)
        services.AddSingleton<IAssistantChatClientProvider, DeepSeekChatClientProvider>();
        services.AddSingleton<AssistantStreamGate>();
        services.AddScoped<AssistantChatHandler>();
        services.AddScoped<AssistantChatOrchestrator>();

        // MCP server (internal-only, JWT-gated) exposing the YtDownloader agent tools
        services.AddMcpServer().WithHttpTransport().WithTools<YtMcpTools>();
    }

    private static void ConfigureBackgroundServices(IServiceCollection services)
    {
        // Background services
        services.AddHostedService<TempFileCleanupService>();
        services.AddHostedService<DownloadWorker>();
    }

    public static void Configure(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseTrustedForwardedHeaders(app.Configuration);
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseSerilogRequestLogging();

        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            if (!string.IsNullOrEmpty(correlationId))
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    await next();
                }
            }
            else
            {
                await next();
            }
        });

        app.UseRouting();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        // Mapped outside /api/v1/yt — the public Gateway (YARP) only proxies
        // /api/v1/yt/**, so /mcp is unreachable through the public Gateway
        // (internal-network + JWT only). No Gateway route is added for /mcp.
        app.MapMcp("/mcp").RequireAuthorization(Permissions.YtDownloaderAccess);

        var yt = app.MapGroup("/api/v1/yt")
            .RequireAuthorization(Permissions.YtDownloaderAccess);

        yt.MapSearchVideos();
        yt.MapSearchHistory();
        yt.MapArchiveSearch();
        yt.MapGetDownloadLink();
        yt.MapDownloadStatus();
        yt.MapDownloadFile();
        yt.MapDownloadHistory();
        yt.MapArchiveDownload();
        yt.MapGetHistory();
        yt.MapStreamPreview();
        yt.MapAssistantChat();

        app.MapGet("/api/v1/yt/health", async (YtDbContext db) =>
            {
                var dbOk = await db.Database.CanConnectAsync();
                return dbOk
                    ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
                    : Results.Problem("Database unavailable", statusCode: 503);
            })
            .WithTags(SwaggerConstants.Tags.System)
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK);
    }
}