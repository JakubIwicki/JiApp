using JiApp.Common.Services;
using JiApp.Gateway;
using JiApp.Gateway.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new GatewaySettings();
builder.Configuration.Bind(settings);
settings.Validate(builder.Environment);

var startup = new Startup(settings, builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);

// Single-instance lease on the shared volume — a duplicate replica exits before serving traffic.
try
{
    SingleInstanceGuard.Acquire("gateway");
}
catch (IOException ex)
{
    app.Logger.LogCritical(ex, "Another gateway instance is already running on the shared volume. Exiting.");
    Environment.Exit(1);
    return;
}

app.Run();