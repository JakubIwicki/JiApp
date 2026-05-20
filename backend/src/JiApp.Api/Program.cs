using System.Reflection;
using FluentValidation;
using JiApp.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

builder.Services.AddScoped<GlobalExceptionMiddleware>();
builder.Services.AddScoped<RequestLoggingMiddleware>();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});

if (app.Environment.IsDevelopment())
{
    app.MapGet("/api/throw", (HttpContext _) => throw new InvalidOperationException("test error"));
}

app.Run();

public partial class Program { }
