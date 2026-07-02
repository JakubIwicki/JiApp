using JiApp.Identity;
using JiApp.Identity.Configuration;
using JiApp.Identity.Persistence;
using JiApp.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new IdentitySettings();
builder.Configuration.Bind(settings);
settings.Validate();

var startup = new Startup(settings, builder.Environment);

startup.ConfigureServices(builder.Services);

var app = builder.Build();

// Auto-apply pending EF migrations on startup (SQLite in all environments today; Npgsql path in Startup.cs exists but is unconfigured)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
    db.Database.Migrate();

    await scope.ServiceProvider.GetRequiredService<IRoleSeeder>().SeedAsync();
}

Startup.Configure(app);

app.Run();