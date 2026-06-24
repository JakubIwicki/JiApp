using JiApp.ImageTools;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

var startup = new Startup();
startup.ConfigureServices(builder.Services);

var app = builder.Build();

Startup.Configure(app);

app.Run();