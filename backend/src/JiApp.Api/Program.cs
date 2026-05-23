using JiApp.Api;
using JiApp.Api.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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

Startup.Configure(app);

app.Run();