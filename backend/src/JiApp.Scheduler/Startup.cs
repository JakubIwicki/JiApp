using System.Text;
using System.Text.Json;
using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using JiApp.Scheduler.Configuration;
using JiApp.Scheduler.Features.Appointments.CreateAppointment;
using JiApp.Scheduler.Features.Appointments.DeleteAppointment;
using JiApp.Scheduler.Features.Appointments.GetAppointment;
using JiApp.Scheduler.Features.Appointments.ListAppointments;
using JiApp.Scheduler.Features.Appointments.UpdateAppointment;
using JiApp.Scheduler.Features.Appointments.UpdateAppointmentStatus;
using JiApp.Scheduler.Features.Boards.AddBoardMember;
using JiApp.Scheduler.Features.Boards.CreateBoard;
using JiApp.Scheduler.Features.Boards.DeleteBoard;
using JiApp.Scheduler.Features.Boards.GetBoard;
using JiApp.Scheduler.Features.Boards.ListBoards;
using JiApp.Scheduler.Features.Boards.RemoveBoardMember;
using JiApp.Scheduler.Features.Boards.UpdateBoard;
using JiApp.Scheduler.Features.Clients.CreateClient;
using JiApp.Scheduler.Features.Clients.DeleteClient;
using JiApp.Scheduler.Features.Clients.GetClient;
using JiApp.Scheduler.Features.Clients.ListClients;
using JiApp.Scheduler.Features.Clients.UpdateClient;
using JiApp.Scheduler.Features.DayTotals;
using JiApp.Scheduler.Features.Expenses.CreateExpense;
using JiApp.Scheduler.Features.Expenses.DeleteExpense;
using JiApp.Scheduler.Features.Expenses.GetExpense;
using JiApp.Scheduler.Features.Expenses.ListExpenses;
using JiApp.Scheduler.Features.Expenses.UpdateExpense;
using JiApp.Scheduler.Features.Reports.ClientReport;
using JiApp.Scheduler.Features.Reports.RevenueReport;
using JiApp.Scheduler.Features.Services.CreateService;
using JiApp.Scheduler.Features.Services.DeleteService;
using JiApp.Scheduler.Features.Services.GetService;
using JiApp.Scheduler.Features.Services.ListServices;
using JiApp.Scheduler.Features.Services.UpdateService;
using JiApp.Scheduler.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Context;

namespace JiApp.Scheduler;

public class Startup(SchedulerSettings settings)
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<GlobalExceptionMiddleware>();

        services.AddDbContext<SchedulerDbContext>(options =>
        {
            if (settings.ConnectionString!.Contains("Host="))
                options.UseNpgsql(settings.ConnectionString);
            else
                options.UseSqlite(settings.ConnectionString);
        });

        services.AddScoped<ISchedulerDbContext>(sp => sp.GetRequiredService<SchedulerDbContext>());

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

        // Client handlers
        services.AddScoped<CreateClientHandler>();
        services.AddScoped<ListClientsHandler>();
        services.AddScoped<GetClientHandler>();
        services.AddScoped<UpdateClientHandler>();
        services.AddScoped<DeleteClientHandler>();

        // Appointment handlers
        services.AddScoped<CreateAppointmentHandler>();
        services.AddScoped<GetAppointmentHandler>();
        services.AddScoped<ListAppointmentsHandler>();
        services.AddScoped<UpdateAppointmentHandler>();
        services.AddScoped<UpdateAppointmentStatusHandler>();
        services.AddScoped<DeleteAppointmentHandler>();

        // Service handlers
        services.AddScoped<CreateServiceHandler>();
        services.AddScoped<GetServiceHandler>();
        services.AddScoped<ListServicesHandler>();
        services.AddScoped<UpdateServiceHandler>();
        services.AddScoped<DeleteServiceHandler>();

        // Expense handlers
        services.AddScoped<CreateExpenseHandler>();
        services.AddScoped<GetExpenseHandler>();
        services.AddScoped<ListExpensesHandler>();
        services.AddScoped<UpdateExpenseHandler>();
        services.AddScoped<DeleteExpenseHandler>();

        // Report handlers
        services.AddScoped<RevenueReportHandler>();
        services.AddScoped<ClientReportHandler>();

        // DayTotals handlers
        services.AddScoped<DayTotalsHandler>();

        // Board validators
        services.AddScoped<IValidator<CreateBoardRequest>, CreateBoardValidator>();
        services.AddScoped<IValidator<UpdateBoardRequest>, UpdateBoardValidator>();
        services.AddScoped<IValidator<AddBoardMemberRequest>, AddBoardMemberValidator>();

        // Client validators
        services.AddScoped<IValidator<CreateClientRequest>, CreateClientValidator>();
        services.AddScoped<IValidator<UpdateClientRequest>, UpdateClientValidator>();

        // Appointment validators
        services.AddScoped<IValidator<CreateAppointmentRequest>, CreateAppointmentValidator>();
        services.AddScoped<IValidator<UpdateAppointmentRequest>, UpdateAppointmentValidator>();
        services.AddScoped<IValidator<UpdateAppointmentStatusRequest>, UpdateAppointmentStatusValidator>();

        // Service validators
        services.AddScoped<IValidator<CreateServiceRequest>, CreateServiceValidator>();
        services.AddScoped<IValidator<UpdateServiceRequest>, UpdateServiceValidator>();

        // Expense validators
        services.AddScoped<IValidator<CreateExpenseRequest>, CreateExpenseValidator>();
        services.AddScoped<IValidator<UpdateExpenseRequest>, UpdateExpenseValidator>();

        // Report validators
        services.AddScoped<IValidator<RevenueReportRequest>, RevenueReportValidator>();
        services.AddScoped<IValidator<ClientReportRequest>, ClientReportValidator>();

        // DayTotals validators
        services.AddScoped<IValidator<DayTotalsRequest>, DayTotalsValidator>();
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

        var scheduler = app.MapGroup("/api/v1/scheduler");

        scheduler.MapGet("/health", async (SchedulerDbContext db) =>
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
        scheduler.MapCreateBoard();
        scheduler.MapGetBoard();
        scheduler.MapUpdateBoard();
        scheduler.MapDeleteBoard();
        scheduler.MapListBoards();
        scheduler.MapAddBoardMember();
        scheduler.MapRemoveBoardMember();

        // Client endpoints
        scheduler.MapCreateClient();
        scheduler.MapListClients();
        scheduler.MapGetClient();
        scheduler.MapUpdateClient();
        scheduler.MapDeleteClient();

        // Appointment endpoints
        scheduler.MapCreateAppointment();
        scheduler.MapGetAppointment();
        scheduler.MapListAppointments();
        scheduler.MapUpdateAppointment();
        scheduler.MapUpdateAppointmentStatus();
        scheduler.MapDeleteAppointment();

        // Service endpoints
        scheduler.MapCreateService();
        scheduler.MapGetService();
        scheduler.MapListServices();
        scheduler.MapUpdateService();
        scheduler.MapDeleteService();

        // Expense endpoints
        scheduler.MapCreateExpense();
        scheduler.MapGetExpense();
        scheduler.MapListExpenses();
        scheduler.MapUpdateExpense();
        scheduler.MapDeleteExpense();

        // Report endpoints
        scheduler.MapRevenueReport();
        scheduler.MapClientReport();

        // DayTotals endpoints
        scheduler.MapDayTotals();
    }
}