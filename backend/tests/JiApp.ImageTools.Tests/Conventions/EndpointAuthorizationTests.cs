using JiApp.Testing.Common.Conventions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.ImageTools.Tests.Conventions;

public sealed class EndpointAuthorizationTests
{
	private sealed class Fixture : IDisposable
	{
		private readonly WebApplicationFactory<JiApp.ImageTools.Program> _factory;

		public Fixture()
		{
			_factory = new WebApplicationFactory<JiApp.ImageTools.Program>()
				.WithWebHostBuilder(builder =>
				{
					builder.UseEnvironment("Test");
				});
		}

		public IEnumerable<EndpointDataSource> DataSources =>
			_factory.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();

		public void Dispose() => _factory.Dispose();

		public static Fixture Init() => new();
	}

	[Fact]
	public void AllEndpoints_RequireAuthorization_UnlessExplicitlyAllowed()
	{
		// Allow-list: ALL ImageTools endpoints are anonymous by design.
		// The module sits behind the Gateway which enforces auth. The service
		// itself has no authentication or authorization middleware.
		// - /api/v1/imagetools/health — health probe (reached from Gateway).
		// - /api/v1/imagetools/ping   — liveness probe (reached from Gateway).
		var allowList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"api/v1/imagetools/health",
			"api/v1/imagetools/ping"
		};

		using var fixture = Fixture.Init();
		var violations = EndpointAuthorizationConvention.CollectUnauthorizedEndpoints(
			fixture.DataSources, allowList);

		Assert.True(violations.Count == 0,
			$"The following {violations.Count} endpoint(s) lack authorization:\n" +
			string.Join("\n", violations));
	}
}
