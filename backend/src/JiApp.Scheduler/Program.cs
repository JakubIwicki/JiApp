using JiApp.Common.Services;
using JiApp.Scheduler;
using JiApp.Scheduler.Configuration;
using JiApp.Scheduler.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new SchedulerSettings();
builder.Configuration.Bind(settings);
settings.Validate(builder.Environment);

var startup = new Startup(settings, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);

// Apply pending migrations (creates DB if needed, supports schema evolution)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SchedulerDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Single-instance lease on the shared volume — a duplicate replica exits before serving traffic.
try
{
    SingleInstanceGuard.Acquire("scheduler");
}
catch (IOException ex)
{
    app.Logger.LogCritical(ex, "Another scheduler instance is already running on the shared volume. Exiting.");
    Environment.Exit(1);
    return;
}

app.Run();