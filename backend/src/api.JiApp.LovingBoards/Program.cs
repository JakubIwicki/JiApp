using api.JiApp.LovingBoards;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Persistence;
using JiApp.Common.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new LovingBoardsSettings();
builder.Configuration.Bind(settings);
settings.Validate();

var startup = new Startup(settings, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);

// Apply pending migrations (creates DB if needed, supports schema evolution)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LovingBoardsDbContext>();
    db.Database.Migrate();
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
}

// Single-instance lease on the shared volume — a duplicate replica exits before serving traffic.
try
{
    SingleInstanceGuard.Acquire("lovingboards");
}
catch (IOException ex)
{
    app.Logger.LogCritical(ex, "Another lovingboards instance is already running on the shared volume. Exiting.");
    Environment.Exit(1);
    return;
}

app.Run();
