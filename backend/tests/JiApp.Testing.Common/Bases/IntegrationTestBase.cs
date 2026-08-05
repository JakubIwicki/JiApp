using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Testing.Common.Bases;

/// <summary>
/// Shared WebApplicationFactory base for full-pipeline integration suites.
/// Sets the "Test" environment (so appsettings.Test.json loads) and applies the
/// WSL inotify polling workaround. A derived factory customizes services by
/// overriding <see cref="ConfigureServices"/>.
/// </summary>
public abstract class IntegrationTestBase<TEntryPoint> : WebApplicationFactory<TEntryPoint>
    where TEntryPoint : class
{
    static IntegrationTestBase()
    {
        // WSL inotify workaround: use polling file watcher instead of
        // FileSystemWatcher-based change notifications. This avoids the
        // "user limit (128) on inotify instances" IOException.
        Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "1");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureServices(ConfigureServices);
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }
}
