using JiApp.Testing.Common.Conventions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.YtDownloader.Tests.Conventions;

public sealed class EndpointAuthorizationTests
{
	private sealed class Fixture : IDisposable
	{
		private readonly WebApplicationFactory<JiApp.YtDownloader.Program> _factory;

		public Fixture()
		{
			_factory = new WebApplicationFactory<JiApp.YtDownloader.Program>()
				.WithWebHostBuilder(builder =>
				{
					builder.UseEnvironment("Test");
					builder.ConfigureAppConfiguration((_, config) =>
					{
						config.AddInMemoryCollection(new Dictionary<string, string?>
						{
							["ConnectionString"] = "DataSource=:memory:",
							["Jwt:Key"] = "test-key-that-is-at-least-32-chars!",
							["Jwt:Issuer"] = "test-issuer",
							["Jwt:Audience"] = "test-audience",
							["Youtube:ApiKey"] = "test-api-key",
							["Youtube:YtDlpPath"] = "/usr/bin/true",
							["Youtube:FfmpegPath"] = "/usr/bin/true",
							["Youtube:MaxResults"] = "30",
							["Youtube:PageSize"] = "10"
						});
					});
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
		// Allow-list: endpoints intentionally anonymous.
		// - /api/v1/yt/health — load-balancer health probe.
		var allowList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"api/v1/yt/health"
		};

		using var fixture = Fixture.Init();
		var violations = EndpointAuthorizationConvention.CollectUnauthorizedEndpoints(
			fixture.DataSources, allowList);

		Assert.True(violations.Count == 0,
			$"The following {violations.Count} endpoint(s) lack authorization:\n" +
			string.Join("\n", violations));
	}
}
