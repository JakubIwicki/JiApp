using JiApp.Gateway;
using JiApp.Gateway.Configuration;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var settings = new GatewaySettings();
builder.Configuration.Bind(settings);
settings.Validate();

var startup = new Startup(settings, builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();
Startup.Configure(app);
app.Run();