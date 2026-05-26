using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using JiApp.Api.Configuration;
using JiApp.Api.Features.Auth.Login;
using JiApp.Api.Features.Auth.Me;
using JiApp.Api.Features.Auth.Register;
using JiApp.Api.Features.Downloads.DownloadFile;
using JiApp.Api.Features.Downloads.ArchiveDownload;
using JiApp.Api.Features.Downloads.DownloadHistory;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using JiApp.Api.Features.History.GetHistory;
using JiApp.Api.Features.Search.ArchiveSearch;
using JiApp.Api.Features.Search.SearchHistory;
using JiApp.Api.Features.Search.SearchVideos;
using JiApp.Api.Middleware;
using JiApp.Api.Services;
using Serilog;
using Asp.Versioning;
using Asp.Versioning.Builder;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Infrastructure.Persistence;
using JiApp.Infrastructure.Repositories;
using JiApp.Infrastructure.Services;
using JiApp.YtApi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace JiApp.Api;

public class Startup(Settings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        ConfigureApiDocumentationAndValidation(services);
        ConfigureApiVersioning(services);
        ConfigureErrorHandling(services);
        ConfigureDatabase(services);
        ConfigureIdentityAndSecurity(services);
        ConfigureRateLimiting(services);
        ConfigureCors(services);
        RegisterApplicationServices(services);
        RegisterRepositories(services);
        ConfigureYoutubeApi(services);
        RegisterInfrastructure(services);
    }

    private static void ConfigureApiDocumentationAndValidation(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(SwaggerConstants.Document.Version, new OpenApiInfo
            {
                Title = SwaggerConstants.Document.Title,
                Version = SwaggerConstants.Document.Version,
            });
            options.DocumentFilter<SwaggerTagDescriptionsFilter>();
        });
    }

    private static void ConfigureApiVersioning(IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ApiVersionReader = new UrlSegmentApiVersionReader();
        });
    }

    private static void ConfigureErrorHandling(IServiceCollection services)
    {
        services.AddTransient<GlobalExceptionMiddleware>();
    }

    private void ConfigureDatabase(IServiceCollection services)
    {
        services.AddDbContext<JiAppDbContext>(options =>
            options.UseSqlite(settings.ConnectionString!));
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

    private void ConfigureIdentityAndSecurity(IServiceCollection services)
    {
        services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<JiAppDbContext>()
            .AddDefaultTokenProviders();

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

        services.AddAuthorization();
    }

    private void ConfigureRateLimiting(IServiceCollection services)
    {
        var rl = settings.RateLimiting!;

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            AddPolicy(RateLimitPolicyNames.Login, rl.Login!);
            AddPolicy(RateLimitPolicyNames.Register, rl.Register!);
            AddPolicy(RateLimitPolicyNames.Health, rl.Health!);
            AddPolicy(RateLimitPolicyNames.DownloadFile, rl.DownloadFile!);
            AddPolicy(RateLimitPolicyNames.SearchVideos, rl.SearchVideos!);
            AddPolicy(RateLimitPolicyNames.SearchHistory, rl.SearchHistory!);
            AddPolicy(RateLimitPolicyNames.DownloadHistory, rl.DownloadHistory!);
            AddPolicy(RateLimitPolicyNames.GetHistory, rl.GetHistory!);
            AddPolicy(RateLimitPolicyNames.Me, rl.Me!);
            AddPolicy(RateLimitPolicyNames.GetDownloadLink, rl.GetDownloadLink!);
            AddPolicy(RateLimitPolicyNames.Preview, rl.Preview!);

            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/json";

                string? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture);
                }

                var body = new ApiErrorResponse(
                    Error: "Too many requests. Please try again later.",
                    RetryAfterSeconds: retryAfter);

                await context.HttpContext.Response.WriteAsync(
                    JsonSerializer.Serialize(body, ApiErrorResponse.JsonOptions), cancellationToken);
            };

            return;

            void AddPolicy(string name, Settings.RateLimitPolicyOptions policy)
            {
                if (policy.SegmentsPerWindow!.Value > 0)
                {
                    options.AddSlidingWindowLimiter(name, opt =>
                    {
                        opt.PermitLimit = policy.PermitLimit!.Value;
                        opt.Window = TimeSpan.FromSeconds(policy.WindowInSeconds!.Value);
                        opt.QueueLimit = policy.QueueLimit!.Value;
                        opt.SegmentsPerWindow = policy.SegmentsPerWindow.Value;
                    });
                }
                else
                {
                    options.AddFixedWindowLimiter(name, opt =>
                    {
                        opt.PermitLimit = policy.PermitLimit!.Value;
                        opt.Window = TimeSpan.FromSeconds(policy.WindowInSeconds!.Value);
                        opt.QueueLimit = policy.QueueLimit!.Value;
                    });
                }
            }
        });
    }

    private void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddSingleton(settings);
        services.AddSingleton<IJwtTokenService>(_ =>
            new JwtTokenService(
                settings.Jwt!.Key!,
                settings.Jwt!.Issuer!,
                settings.Jwt!.Audience!,
                settings.Jwt!.ExpireMinutes!.Value));
        services.AddSingleton<ITempFileStore, TempFileStore>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddMemoryCache();

        services.AddScoped<IValidator<RegisterRequest>, RegisterValidator>();
        services.AddScoped<IValidator<LoginRequest>, LoginValidator>();
        services.AddScoped<IValidator<DownloadRequest>, GetDownloadLinkValidator>();
        services.AddScoped<IValidator<DownloadHistoryRequest>, DownloadHistoryValidator>();
        services.AddScoped<IValidator<SearchVideosRequest>, SearchVideosValidator>();
        services.AddScoped<IValidator<SearchHistoryRequest>, SearchHistoryValidator>();
        services.AddScoped<IValidator<GetHistoryRequest>, GetHistoryValidator>();
        services.AddScoped<IValidator<ArchiveDownloadRequest>, ArchiveDownloadValidator>();
        services.AddScoped<IValidator<ArchiveSearchRequest>, ArchiveSearchValidator>();

        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<MeHandler>();
        services.AddScoped<SearchVideosHandler>();
        services.AddScoped<SearchHistoryHandler>();
        services.AddScoped<GetDownloadLinkHandler>();
        services.AddScoped<DownloadFileHandler>();
        services.AddScoped<DownloadHistoryHandler>();
        services.AddScoped<GetHistoryHandler>();
        services.AddScoped<ArchiveDownloadHandler>();
        services.AddScoped<ArchiveSearchHandler>();
    }

    private static void RegisterRepositories(IServiceCollection services)
    {
        services.AddScoped<ISearchHistoryRepository, SearchHistoryRepository>();
        services.AddScoped<IDownloadHistoryRepository, DownloadHistoryRepository>();
        services.AddScoped<IEventLogRepository, EventLogRepository>();
    }

    private void ConfigureYoutubeApi(IServiceCollection services)
    {
        services.AddSingleton(settings.Youtube!);
        services.AddSingleton<IYoutubeClient>(_ =>
            new YoutubeClient(
                settings.Youtube!.ApiKey!,
                settings.Youtube!.YtDlpPath!,
                settings.Youtube!.FfmpegPath!));
    }

    private static void RegisterInfrastructure(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddHostedService<TempFileCleanupService>();
    }

    public static void Configure(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        app.UseMiddleware<GlobalExceptionMiddleware>();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            app.Use(async (context, next) =>
            {
                app.Logger.LogInformation("--> {Method} {Path}{QueryString}",
                    context.Request.Method,
                    context.Request.Path,
                    context.Request.QueryString);

                var sw = Stopwatch.StartNew();
                await next();
                sw.Stop();

                app.Logger.LogInformation("<-- {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    context.Request.Method,
                    context.Request.Path,
                    context.Response.StatusCode,
                    sw.ElapsedMilliseconds);
            });
        }

        app.UseRouting();

        app.UseHttpsRedirection();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            await next();
        });

        app.UseRateLimiter();
        app.UseCors();
        app.UseAuthentication();
        app.UseAuthorization();

        var apiVersionSet = app.NewApiVersionSet()
            .HasApiVersion(new ApiVersion(1))
            .Build();

        var v1 = app.MapGroup("/api/v{version:apiVersion}")
            .WithApiVersionSet(apiVersionSet)
            .HasApiVersion(1);

        v1.MapRegister();
        v1.MapLogin();
        v1.MapMe();
        v1.MapSearchVideos();
        v1.MapSearchHistory();
        v1.MapGetDownloadLink();
        v1.MapDownloadFile();
        v1.MapDownloadHistory();
        v1.MapGetHistory();
        v1.MapArchiveDownload();
        v1.MapArchiveSearch();

        v1.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }))
            .WithTags(SwaggerConstants.Tags.System)
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK)
            .RequireRateLimiting(RateLimitPolicyNames.Health)
            .HasApiVersion(1);

        if (app.Environment.IsDevelopment())
        {
            v1.MapGet("/throw", _ => throw new InvalidOperationException("test error"))
                .HasApiVersion(1);
        }
    }
}