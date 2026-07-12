using JiApp.Testing.Common.Conventions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Identity.Tests.Conventions;

public sealed class EndpointAuthorizationTests
{
	private sealed class Fixture : IDisposable
	{
		private readonly WebApplicationFactory<JiApp.Identity.Program> _factory;

		public Fixture()
		{
			_factory = new WebApplicationFactory<JiApp.Identity.Program>()
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
							["Jwt:AccessTokenExpireMinutes"] = "60",
							["Jwt:RefreshTokenExpireDays"] = "7"
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
		// - /api/v1/auth/register — new user self-registration.
		// - /api/v1/auth/login    — obtain JWT token.
		// - /api/v1/auth/refresh  — refresh JWT token via refresh token.
		// - /api/v1/auth/logout   — revoke refresh token.
		// - /api/v1/auth/health   — load-balancer health probe.
		var allowList = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"api/v1/auth/register",
			"api/v1/auth/login",
			"api/v1/auth/refresh",
			"api/v1/auth/logout",
			"api/v1/auth/health"
		};

		using var fixture = Fixture.Init();
		var violations = EndpointAuthorizationConvention.CollectUnauthorizedEndpoints(
			fixture.DataSources, allowList);

		Assert.True(violations.Count == 0,
			$"The following {violations.Count} endpoint(s) lack authorization:\n" +
			string.Join("\n", violations));
	}
}
