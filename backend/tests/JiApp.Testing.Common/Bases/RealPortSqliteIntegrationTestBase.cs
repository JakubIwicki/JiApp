using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Testing.Common.Bases;

/// <summary>
/// SQLite integration host that listens on a real ephemeral Kestrel port instead
/// of the in-process TestServer. Used when the SUT must drive an actual TCP
/// connection (SSE streaming, cross-host HTTP) that the TestServer host cannot
/// surface.
/// <para>
/// Call <see cref="StartServer"/> before reading <see cref="RealBaseAddress"/>.
/// The <see cref="WebApplicationFactory.Server"/> property THROWS under Kestrel
/// hosting — never use it. <see cref="WebApplicationFactory.ClientOptions"/>.
/// BaseAddress is also NOT populated for a Kestrel host (it stays at the factory
/// default, http://localhost), so the real address is read from the running
/// server's <see cref="IServerAddressesFeature"/>.
/// </para>
/// </summary>
public abstract class RealPortSqliteIntegrationTestBase<TEntryPoint, TDbContext>
    : ModuleSqliteIntegrationTestBase<TEntryPoint, TDbContext>
    where TEntryPoint : class
    where TDbContext : DbContext
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        // Port 0 = ephemeral. ListenLocalhost(0) rejects dynamic ports; bind the
        // loopback address explicitly so Kestrel picks a free port.
        builder.UseKestrel(options => options.Listen(IPAddress.Loopback, 0));
    }

    /// <summary>
    /// The real base address (http://127.0.0.1:&lt;ephemeral-port&gt;) once the
    /// server has been started.
    /// </summary>
    public Uri RealBaseAddress
    {
        get
        {
            var addresses = Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
            var address = addresses.FirstOrDefault(a => a.StartsWith("http://127.0.0.1:"))
                          ?? addresses.FirstOrDefault(a => a.StartsWith("http://"));
            if (address is null)
                throw new InvalidOperationException(
                    "Real-port host has no bound HTTP address. Call StartServer() first.");
            return new Uri(address.EndsWith('/') ? address : address + "/");
        }
    }
}
