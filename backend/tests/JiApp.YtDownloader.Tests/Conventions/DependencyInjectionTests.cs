using System.Reflection;
using JiApp.Testing.Common.Conventions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JiApp.YtDownloader.Tests.Conventions;

public sealed class DependencyInjectionTests
{
	private sealed class Fixture
	{
		public ServiceCollection Services { get; }
		public ServiceProvider Provider { get; }
		public Assembly[] ProductionAssemblies { get; }

		public Fixture()
		{
			var config = new ConfigurationBuilder()
				.AddInMemoryCollection(new Dictionary<string, string?>
				{
					["ConnectionString"] = "DataSource=:memory:",
					["Jwt:Key"] = "test-key-that-is-at-least-32-chars!",
					["Jwt:Issuer"] = "test-issuer",
					["Jwt:Audience"] = "test-audience",
					["App:PreviewDurationSeconds"] = "10",
					["Youtube:ApiKey"] = "test-api-key",
					["Youtube:YtDlpPath"] = "/usr/bin/true",
					["Youtube:FfmpegPath"] = "/usr/bin/true",
					["Youtube:MaxResults"] = "30",
					["Youtube:PageSize"] = "10"
				})
				.Build();

			var settings = new Settings();
			config.Bind(settings);
			settings.Validate();

			var envMock = new Mock<IWebHostEnvironment>();
			envMock.SetupGet(e => e.EnvironmentName).Returns("Test");

			Services = new ServiceCollection();
			Services.AddLogging();
			Services.AddSingleton<IConfiguration>(config);
			Services.AddSingleton(envMock.Object);

			var startup = new JiApp.YtDownloader.Startup(settings, envMock.Object);
			startup.ConfigureServices(Services);

			Provider = Services.BuildServiceProvider();

			ProductionAssemblies =
			[
				typeof(JiApp.YtDownloader.Startup).Assembly,
				typeof(JiApp.Common.Abstractions.ICurrentUserService).Assembly,
				typeof(JiApp.YtApi.IYoutubeClient).Assembly
			];
		}

		public static Fixture Init() => new();
	}

	[Fact]
	public void AllRegisteredServices_AreResolvable()
	{
		var fixture = Fixture.Init();
		var result = DependencyInjectionConvention.CollectUnresolvableServices(
			fixture.Services, fixture.Provider, fixture.ProductionAssemblies);

		Assert.True(result.ScannedCount > 0,
			"0 services matched — the fitness test ran vacuously");
		Assert.True(result.Violations.Count == 0,
			$"The following {result.Violations.Count} service(s) could not be resolved:\n" +
			string.Join("\n", result.Violations));
	}
}
