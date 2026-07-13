using JiApp.Common.Middleware;
using JiApp.Common.Resilience;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace JiApp.Common.Services;

public static class SecurityStampRecheckExtensions
{
	public static IServiceCollection AddSecurityStampRecheck(
		this IServiceCollection services, string? identityBaseUrl, IWebHostEnvironment env)
	{
		services.TryAddSingleton<IRetryPolicyFactory, RetryPolicyFactory>();
		services.AddHttpContextAccessor();

		if (!string.IsNullOrEmpty(identityBaseUrl))
		{
			services.AddHttpClient<ISecurityStampValidator, RemoteSecurityStampValidator>(client =>
			{
				client.BaseAddress = new Uri(identityBaseUrl);
				client.Timeout = TimeSpan.FromSeconds(5);
			});
		}
		else if (env.IsDevelopment())
		{
			services.AddSingleton<ISecurityStampValidator, NoOpSecurityStampValidator>();
		}
		else
		{
			throw new InvalidOperationException(
				"IdentityBaseUrl must be configured in non-Development environments for security-stamp recheck.");
		}

		services.AddScoped<SecurityStampRecheckFilter>();

		return services;
	}
}
