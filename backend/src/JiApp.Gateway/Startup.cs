using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Gateway.Configuration;
using JiApp.Gateway.HealthDashboard;
using JiApp.Gateway.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace JiApp.Gateway;

public class Startup(GatewaySettings settings, IConfiguration configuration, IWebHostEnvironment env)
{
    public void ConfigureServices(IServiceCollection services)
    {
        // JWT Bearer authentication — validates tokens issued by JiApp-Identity
        // Validate() guarantees Jwt is configured at this point.
        var jwt = settings.Jwt ?? throw new InvalidOperationException("Jwt must be configured");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidAlgorithms = ["HS256"],
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
                    },
                };
            });

        services.AddAuthorization();

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

        // Rate limiting — reads policy config from GatewaySettings
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            foreach (var (sectionName, policyConfig) in settings.RateLimiting!)
            {
                var policyName = sectionName + "Policy";
                var config = policyConfig; // capture per-iteration to avoid loop-variable closure
                if (config.SegmentsPerWindow > 0)
                {
                    options.AddPolicy(policyName, httpContext =>
                        RateLimitPartition.GetSlidingWindowLimiter(
                            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = config.PermitLimit,
                                Window = TimeSpan.FromSeconds(config.WindowInSeconds),
                                QueueLimit = config.QueueLimit,
                                SegmentsPerWindow = config.SegmentsPerWindow,
                            }));
                }
                else
                {
                    options.AddPolicy(policyName, httpContext =>
                        RateLimitPartition.GetFixedWindowLimiter(
                            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                            _ => new FixedWindowRateLimiterOptions
                            {
                                PermitLimit = config.PermitLimit,
                                Window = TimeSpan.FromSeconds(config.WindowInSeconds),
                                QueueLimit = config.QueueLimit,
                            }));
                }
            }

            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Startup>>();
                logger.LogWarning("Rate limit exceeded for {Path} by client {RemoteIp}",
                    context.HttpContext.Request.Path, context.HttpContext.Connection.RemoteIpAddress);

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
        });

        // YARP reverse proxy — in dev, bypass SSL validation for self-signed certs
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .ConfigureHttpClient((context, handler) =>
            {
                handler.SslOptions.RemoteCertificateValidationCallback =
                    (sender, cert, chain, errors) => true;
            });

        // Rate limit policy service — endpoint manipulation for rate limiting
        services.AddSingleton<RateLimitPolicyService>();

        // Global exception middleware — catches unhandled exceptions, returns JSON
        services.AddScoped<GlobalExceptionMiddleware>();

        // HttpClient for health dashboard — bypass SSL for dev self-signed certs
        services.AddHttpClient("healthCheck")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });
    }

    public static void Configure(WebApplication app)
    {
        // Global exception handler — catches unhandled exceptions before any middleware
        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseSerilogRequestLogging();

        // Correlation ID middleware — reads or generates X-Correlation-ID, forwards downstream
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                ?? Guid.NewGuid().ToString("N");

            context.Request.Headers["X-Correlation-ID"] = correlationId;
            context.Response.Headers["X-Correlation-ID"] = correlationId;
            context.Items["CorrelationId"] = correlationId;

            await next();
        });

        app.UseRouting();
        app.UseCors();
        app.UseMiddleware<global::JiApp.Gateway.RateLimiting.RateLimitPolicySelector>();
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapReverseProxy();

        app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
        app.MapGet("/health/live", () => Results.Ok(new { status = "alive" }));
        app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

        // Health dashboard — dev only
        if (app.Environment.IsDevelopment())
        {
            HealthDashboardEndpoint.MapHealthDashboard(
                app, "https://localhost:6701", "https://localhost:6702", "https://localhost:6703",
                "https://localhost:6704", "https://localhost:6705");
        }
    }
}