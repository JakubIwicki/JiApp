using System.Text;
using System.Text.Json;
using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Common.Models;
using JiApp.Identity.Configuration;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using JiApp.Identity.Features.Auth.Login;
using JiApp.Identity.Features.Auth.Logout;
using JiApp.Identity.Features.Auth.Me;
using JiApp.Identity.Features.Auth.Refresh;
using JiApp.Identity.Features.Auth.Register;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

namespace JiApp.Identity;

public class Startup(IdentitySettings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddTransient<GlobalExceptionMiddleware>();

        var jwt = settings.GetRequiredJwt();
        var connectionString = settings.ConnectionString ??
                               throw new InvalidOperationException("ConnectionString not configured");

        services.AddDbContext<IdentityDbContext>(options =>
        {
            if (connectionString.Contains("Host="))
                options.UseNpgsql(connectionString);
            else
                options.UseSqlite(connectionString);
        });

        services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.AllowedForNewUsers = true;
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = JwtTokenService.CreateValidationParameters(
                    jwt.ValidatedKey, jwt.ValidatedIssuer, jwt.ValidatedAudience);

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

        services.AddSingleton(settings);

        services.AddSingleton<IJwtTokenService>(_ =>
            new JwtTokenService(
                jwt.ValidatedKey,
                jwt.ValidatedIssuer,
                jwt.ValidatedAudience,
                jwt.ValidatedAccessTokenExpireMinutes));

        services.AddScoped<IRefreshTokenService>(sp =>
        {
            var dbContext = sp.GetRequiredService<IdentityDbContext>();
            return new RefreshTokenService(dbContext, jwt.ValidatedRefreshTokenExpireDays);
        });

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IUserModuleGrantService, UserModuleGrantService>();

        services.AddHttpContextAccessor();

        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<MeHandler>();

        services.AddValidatorsFromAssemblyContaining<RegisterValidator>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("Register", config =>
            {
                config.PermitLimit = 5;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("Login", config =>
            {
                config.PermitLimit = 10;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("Refresh", config =>
            {
                config.PermitLimit = 10;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("Logout", config =>
            {
                config.PermitLimit = 10;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 0;
            });
        });

        services.AddHostedService<RefreshTokenCleanupService>();
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
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        var auth = app.MapGroup("/api/v1/auth");

        auth.MapRegister();
        auth.MapLogin();
        auth.MapRefresh();
        auth.MapLogout();
        auth.MapMe();

        auth.MapGet("/health", async (IdentityDbContext db) =>
            {
                var dbOk = await db.Database.CanConnectAsync();
                return dbOk
                    ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
                    : Results.Problem("Database unavailable", statusCode: 503);
            })
            .WithTags(SwaggerConstants.Tags.System)
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK);

        if (app.Environment.IsDevelopment())
        {
            auth.MapGet("/throw", _ => throw new InvalidOperationException("test error"));
        }
    }
}