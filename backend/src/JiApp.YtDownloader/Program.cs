using JiApp.Common.Services;
using JiApp.YtDownloader;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new Settings();
builder.Configuration.Bind(settings);
settings.Validate();

var startup = new Startup(settings, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Auto-apply pending EF migrations on startup (dev: SQLite, prod: PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();
    db.Database.Migrate();
}

Startup.Configure(app);

// Single-instance lease on the shared volume — a duplicate replica exits before serving traffic.
try
{
    SingleInstanceGuard.Acquire("ytdownloader");
}
catch (IOException ex)
{
    app.Logger.LogCritical(ex, "Another ytdownloader instance is already running on the shared volume. Exiting.");
    Environment.Exit(1);
    return;
}

app.Run();