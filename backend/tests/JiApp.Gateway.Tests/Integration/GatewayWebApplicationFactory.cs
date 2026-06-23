using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace JiApp.Gateway.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory for the Gateway that sets the environment to
/// "Test" so appsettings.Test.json is loaded (provides Jwt:Key) instead of
/// appsettings.Development.json (which references a Kestrel dev cert path
/// that doesn't exist in the test runner environment).
/// Also sets DOTNET_USE_POLLING_FILE_WATCHER=1 to work around WSL inotify limits.
/// </summary>
public class GatewayWebApplicationFactory : WebApplicationFactory<JiApp.Gateway.Program>
{
    static GatewayWebApplicationFactory()
    {
        // WSL inotify workaround: use polling file watcher instead of
        // FileSystemWatcher-based change notifications. This avoids the
        // "user limit (128) on inotify instances" IOException.
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
    }
}