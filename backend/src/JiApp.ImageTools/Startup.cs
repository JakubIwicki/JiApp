namespace JiApp.ImageTools;

public class Startup()
{
    public void ConfigureServices(IServiceCollection services)
    {
        // No service-level middleware registration needed.
        // Authentication, CORS, rate limiting are all handled by Gateway.

        services.AddRouting();
    }

    public static void Configure(WebApplication app)
    {
        var tools = app.MapGroup("/api/v1/imagetools");

        tools.MapGet("/health", () =>
            Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

        tools.MapGet("/ping", () =>
            Results.Ok(new { module = "image-tools", status = "ok" }));
    }
}