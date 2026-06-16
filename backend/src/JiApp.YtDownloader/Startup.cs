using System.Text;
using System.Text.Json;
using FluentValidation;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using JiApp.YtApi;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.ArchiveDownload;
using JiApp.YtDownloader.Features.ArchiveSearch;
using JiApp.YtDownloader.Features.DownloadFile;
using JiApp.YtDownloader.Features.DownloadHistory;
using JiApp.YtDownloader.Features.GetDownloadLink;
using JiApp.YtDownloader.Features.GetHistory;
using JiApp.YtDownloader.Features.SearchHistory;
using JiApp.YtDownloader.Features.SearchVideos;
using JiApp.YtDownloader.Features.StreamPreview;
using JiApp.YtDownloader.Persistence;
using JiApp.YtDownloader.Repositories;
using JiApp.YtDownloader.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

namespace JiApp.YtDownloader;

public class Startup(Settings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddTransient<GlobalExceptionMiddleware>();

        services.AddDbContext<YtDbContext>(options =>
        {
            if (settings.ConnectionString!.Contains("Host="))
                options.UseNpgsql(settings.ConnectionString);
            else
                options.UseSqlite(settings.ConnectionString);
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = settings.Jwt!.Issuer!,
                    ValidAudience = settings.Jwt!.Audience!,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.Jwt!.Key!)),
                };

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

        services.AddAuthorizationBuilder()
            .AddPolicy("module:YtDownloader", policy =>
                policy.RequireClaim("module", Modules.YtDownloader, Modules.FullAccess));

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true);
            });
        });

        // Repositories
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
        services.AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>();
        // Services
        services.AddSingleton<ITempFileStore, TempFileStore>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IYoutubeClient>(_ =>
            new YoutubeClient(
                settings.Youtube!.ApiKey!,
                settings.Youtube!.YtDlpPath!,
                settings.Youtube!.FfmpegPath!,
                settings.Youtube!.CookiesFile,
                settings.Youtube!.CookiesFromBrowser));

        services.AddSingleton(settings);
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 1024;
            options.CompactionPercentage = 0.25;
        });
        services.AddHttpContextAccessor();

        // Validators
        services.AddScoped<IValidator<SearchVideosRequest>, SearchVideosValidator>();
        services.AddScoped<IValidator<SearchHistoryRequest>, SearchHistoryValidator>();
        services.AddScoped<IValidator<ArchiveSearchRequest>, ArchiveSearchValidator>();
        services.AddScoped<IValidator<DownloadRequest>, GetDownloadLinkValidator>();
        services.AddScoped<IValidator<DownloadHistoryRequest>, DownloadHistoryValidator>();
        services.AddScoped<IValidator<ArchiveDownloadRequest>, ArchiveDownloadValidator>();
        services.AddScoped<IValidator<GetHistoryRequest>, GetHistoryValidator>();

        // Handlers
        services.AddScoped<SearchVideosHandler>();
        services.AddScoped<SearchHistoryHandler>();
        services.AddScoped<ArchiveSearchHandler>();
        services.AddScoped<GetDownloadLinkHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<DownloadHistoryHandler>();
        services.AddScoped<ArchiveDownloadHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddScoped<StreamPreviewHandler>();

        // Background services
        services.AddHostedService<TempFileCleanupService>();
    }

    public static void Configure(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
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

        var yt = app.MapGroup("/api/v1/yt")
            .RequireAuthorization("module:YtDownloader");

        yt.MapSearchVideos();
        yt.MapSearchHistory();
        yt.MapArchiveSearch();
        yt.MapGetDownloadLink();
        yt.MapDownloadFile();
        yt.MapDownloadHistory();
        yt.MapArchiveDownload();
        yt.MapGetHistory();
        yt.MapStreamPreview();

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