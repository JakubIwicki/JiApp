using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Testing.Common.Conventions;

/// <summary>
/// Shared convention: every type registered in the composition root that belongs
/// to a production assembly must be resolvable from a scope. Collects all failures
/// so a single assertion reports every unresolvable registration.
/// </summary>
public static class DependencyInjectionConvention
{
	/// <summary>
	/// Walk every descriptor in <paramref name="services"/> whose service type
	/// belongs to a JiApp assembly, attempt resolution through a scope, and
	/// collect failures.
	/// </summary>
	/// <param name="services">The service collection built from the production Startup.</param>
	/// <param name="provider">A built provider (root scope) from the same collection.</param>
	/// <param name="productionAssemblies">
	/// Only descriptors whose service type belongs to one of these assemblies are checked.
	/// </param>
	/// <returns>One violation string per unresolvable descriptor, empty if all resolved.</returns>
	public static List<string> CollectUnresolvableServices(
		IServiceCollection services,
		IServiceProvider provider,
		Assembly[] productionAssemblies)
	{
		var violations = new List<string>();
		var assemblyNames = new HashSet<string>(
			productionAssemblies.Select(a => a.GetName().Name!),
			StringComparer.OrdinalIgnoreCase);

		using var scope = provider.CreateScope();

		foreach (var descriptor in services)
		{
			// Open generics cannot be resolved without a concrete type parameter.
			if (descriptor.ServiceType.IsGenericTypeDefinition)
				continue;

			var asmName = descriptor.ServiceType.Assembly.GetName().Name;
			if (asmName is null || !assemblyNames.Contains(asmName))
				continue;

			try
			{
				var resolved = scope.ServiceProvider.GetService(descriptor.ServiceType);
				if (resolved is null)
				{
					violations.Add(
						$"  {descriptor.ServiceType.FullName} " +
						$"(lifetime: {descriptor.Lifetime}) — GetService returned null");
				}
			}
			catch (Exception ex)
			{
				violations.Add(
					$"  {descriptor.ServiceType.FullName} " +
					$"(lifetime: {descriptor.Lifetime}) — threw {ex.GetType().Name}: " +
					$"{ex.Message.Split('\n')[0]}");
			}
		}

		return violations;
	}
}
