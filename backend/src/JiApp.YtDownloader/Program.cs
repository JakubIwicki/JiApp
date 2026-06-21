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

var startup = new Startup(settings);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Auto-apply pending EF migrations on startup (dev: SQLite, prod: PostgreSQL)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<YtDbContext>();
    db.Database.Migrate();
}

Startup.Configure(app);

app.Run();