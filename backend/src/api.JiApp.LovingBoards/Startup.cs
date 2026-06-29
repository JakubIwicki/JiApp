using System.Text;
using System.Text.Json;
using FluentValidation;
using JiApp.Common;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Boards.AddBoardMember;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;
using api.JiApp.LovingBoards.Features.Boards.DeleteBoard;
using api.JiApp.LovingBoards.Features.Boards.GetBoard;
using api.JiApp.LovingBoards.Features.Boards.ListBoards;
using api.JiApp.LovingBoards.Features.Boards.RemoveBoardMember;
using api.JiApp.LovingBoards.Features.Boards.UpdateBoard;
using api.JiApp.LovingBoards.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

namespace api.JiApp.LovingBoards;

public class Startup(LovingBoardsSettings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<GlobalExceptionMiddleware>();

        services.AddDbContext<LovingBoardsDbContext>(options =>
        {
            if (settings.ConnectionString!.Contains("Host="))
                options.UseNpgsql(settings.ConnectionString);
            else
                options.UseSqlite(settings.ConnectionString);
        });

        services.AddScoped<ILovingBoardsDbContext>(sp => sp.GetRequiredService<LovingBoardsDbContext>());

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
            .AddPolicy("module:LovingBoards", policy =>
                policy.RequireClaim("module", Modules.LovingBoards, Modules.FullAccess));

        // CORS
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();

                if (settings.CorsAllowedOrigins is { Length: > 0 } origins)
                    policy.SetIsOriginAllowed(origin => origins.Contains(origin));
                else
                    policy.SetIsOriginAllowed(_ => true);
            });
        });

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton(settings);
        services.AddHttpContextAccessor();

        // Board handlers
        services.AddScoped<CreateBoardHandler>();
        services.AddScoped<GetBoardHandler>();
        services.AddScoped<UpdateBoardHandler>();
        services.AddScoped<DeleteBoardHandler>();
        services.AddScoped<ListBoardsHandler>();
        services.AddScoped<AddBoardMemberHandler>();
        services.AddScoped<RemoveBoardMemberHandler>();

        // Board validators
        services.AddScoped<IValidator<CreateBoardRequest>, CreateBoardValidator>();
        services.AddScoped<IValidator<UpdateBoardRequest>, UpdateBoardValidator>();
        services.AddScoped<IValidator<AddBoardMemberRequest>, AddBoardMemberValidator>();
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

        var lovingboards = app.MapGroup("/api/v1/lovingboards")
            .RequireAuthorization("module:LovingBoards");

        app.MapGet("/api/v1/lovingboards/health", async (LovingBoardsDbContext db) =>
            {
                var dbOk = await db.Database.CanConnectAsync();
                return dbOk
                    ? Results.Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow })
                    : Results.Problem("Database unavailable", statusCode: 503);
            })
            .WithTags("System")
            .WithSummary("Health check")
            .Produces(StatusCodes.Status200OK);

        // Board endpoints
        lovingboards.MapCreateBoard();
        lovingboards.MapGetBoard();
        lovingboards.MapUpdateBoard();
        lovingboards.MapDeleteBoard();
        lovingboards.MapListBoards();
        lovingboards.MapAddBoardMember();
        lovingboards.MapRemoveBoardMember();
    }
}
