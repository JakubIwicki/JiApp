using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Api.Features.Auth.Login;
using JiApp.Api.Features.Auth.Me;
using JiApp.Api.Features.Auth.Register;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Api.Middleware;
using JiApp.Api.Services;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Persistence;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.YtApi;
using JiApp.YtApi.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Api;

public class Startup(IConfiguration configuration)
{
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureApiDocumentationAndValidation(services);
        ConfigureErrorHandling(services);
        ConfigureDatabase(services);
        ConfigureIdentityAndSecurity(services);
        ConfigureCors(services);
        ConfigureRateLimiting(services);
        RegisterApplicationServices(services);
        RegisterRepositories(services);
        ConfigureYoutubeApi(services);
        RegisterInfrastructure(services);
    }

    private static void ConfigureApiDocumentationAndValidation(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    }

    private static void ConfigureErrorHandling(IServiceCollection services)
    {
        services.AddTransient<GlobalExceptionMiddleware>();
        services.AddTransient<RequestLoggingMiddleware>();
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        var baseDir = AppContext.BaseDirectory;
        var connectionString = (configuration.GetConnectionString("JiDb")
                                ?? throw new InvalidOperationException("Connection string 'JiDb' is not configured."))
            .Replace("${BaseDirectory}", baseDir);
        services.AddDbContext<JiAppDbContext>(options =>
            options.UseSqlite(connectionString));

        var appBaseDir = configuration["App:BaseDirectory"];
        if (!string.IsNullOrEmpty(appBaseDir))
        {
            configuration["App:BaseDirectory"] = appBaseDir.Replace("${BaseDirectory}", baseDir);
        }
    }

    private void ConfigureIdentityAndSecurity(IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<JiAppDbContext>()
            .AddDefaultTokenProviders();

        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured.");

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
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
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

        services.AddAuthorization();
    }

    private static void ConfigureCors(IServiceCollection services)
    {
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
    }

    private void ConfigureRateLimiting(IServiceCollection services)
    {
        services.Configure<RateLimitingOptions>(
            configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter(RateLimitPolicyNames.Login, opt =>
            {
                var s = GetPolicySettings("Login", new RateLimitPolicyOptions { PermitLimit = 5, WindowInSeconds = 60 });
                opt.PermitLimit = s.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(s.WindowInSeconds);
                opt.QueueLimit = s.QueueLimit;
            });

            options.AddFixedWindowLimiter(RateLimitPolicyNames.Register, opt =>
            {
                var s = GetPolicySettings("Register", new RateLimitPolicyOptions { PermitLimit = 3, WindowInSeconds = 60 });
                opt.PermitLimit = s.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(s.WindowInSeconds);
                opt.QueueLimit = s.QueueLimit;
            });

            options.AddFixedWindowLimiter(RateLimitPolicyNames.Health, opt =>
            {
                var s = GetPolicySettings("Health", new RateLimitPolicyOptions { PermitLimit = 30, WindowInSeconds = 60 });
                opt.PermitLimit = s.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(s.WindowInSeconds);
                opt.QueueLimit = s.QueueLimit;
            });

            options.AddSlidingWindowLimiter(RateLimitPolicyNames.DownloadFile, opt =>
            {
                var s = GetPolicySettings("DownloadFile", new() { PermitLimit = 10, WindowInSeconds = 60 });
                opt.PermitLimit = s.PermitLimit;
                opt.Window = TimeSpan.FromSeconds(s.WindowInSeconds);
                opt.QueueLimit = s.QueueLimit;
                opt.SegmentsPerWindow = s.SegmentsPerWindow;
            });

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                string? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue.TotalSeconds.ToString("F0");
                }

                var body = new ApiErrorResponse(
                    Error: "Too many requests. Please try again later.",
                    RetryAfterSeconds: retryAfter);

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(body, ApiErrorResponse.JsonOptions), cancellationToken);
            };
        });
    }

    private static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<ITempFileStore, TempFileStore>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<MeHandler>();
        services.AddScoped<SearchVideosHandler>();
        services.AddScoped<SearchHistoryHandler>();
        services.AddScoped<GetDownloadLinkHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<DownloadHistoryHandler>();
        services.AddScoped<GetHistoryHandler>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
        services.AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();
    }

    private void ConfigureYoutubeApi(IServiceCollection services)
    {
        var apiKey = configuration["Youtube:api-key"] ?? string.Empty;
        var ytDlpPath = configuration["Youtube:yt-dlp"] ?? string.Empty;
        var ffmpegPath = configuration["Youtube:ffmpeg"] ?? string.Empty;
        services.AddSingleton(new YoutubeSettings(apiKey, ytDlpPath, ffmpegPath));
        services.AddSingleton<IYoutubeClient, YoutubeClient>();
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHostedService<TempFileCleanupService>();
    }

    private RateLimitPolicyOptions GetPolicySettings(string name, RateLimitPolicyOptions fallback)
    {
        return configuration
            .GetSection($"{RateLimitingOptions.SectionName}:{name}")
            .Get<RateLimitPolicyOptions>() ?? fallback;
    }

    public static void Configure(WebApplication app)
    {
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseRouting();
        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapRegister();
        app.MapLogin();
        app.MapMe();
        app.MapSearchVideos();
        app.MapSearchHistory();
        app.MapGetDownloadLink();
        app.MapDownloadFile();
        app.MapDownloadHistory();
        app.MapGetHistory();

        app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithTags("System")
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK)
            .RequireRateLimiting(RateLimitPolicyNames.Health);

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/api/throw", _ => throw new InvalidOperationException("test error"));
        }
    }
}