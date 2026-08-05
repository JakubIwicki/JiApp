using System.Reflection;
using JiApp.Common.Abstractions;
using JiApp.Common.Authentication;
using JiApp.Common.Authorization;
using JiApp.Common.Middleware;
using JiApp.Common.Resilience;
using JiApp.Common.Services;

namespace JiApp.Common.Tests.Conventions;

public sealed class CommonCoverageConventionTests
{
    // Whitelist of kernel types every change to JiApp.Common must keep tested.
    // Entries are typeof() references, so a deleted kernel type breaks this
    // file's build — the fitness test cannot silently skip a missing type.
    private static readonly Type[] KernelTypes =
    [
        typeof(Result<>),
        typeof(GlobalExceptionMiddleware),
        typeof(CurrentUserService),
        typeof(PermissionAuthorizationHandler),
        typeof(RetryPolicyFactory),
        typeof(SecurityStampRecheckFilter),
        typeof(RemoteSecurityStampValidator),
        typeof(SingleInstanceGuard),
        typeof(JwtSettings),
        typeof(TokenValidationParametersFactory)
    ];

    [Fact]
    public void AllKernelTypes_HaveTestClasses()
    {
        Assert.True(KernelTypes.Length > 0,
            "0 kernel types scanned — the fitness test ran vacuously");

        var commonAssembly = typeof(Result<>).Assembly;
        Assert.Equal("JiApp.Common", commonAssembly.GetName().Name);

        var testClassNames = typeof(CommonCoverageConventionTests).Assembly
            .GetExportedTypes()
            .Select(t => t.Name)
            .ToHashSet();

        var result = CollectMissingTestClasses(KernelTypes, testClassNames, commonAssembly);

        Assert.True(result.ScannedCount == KernelTypes.Length,
            $"Scanned {result.ScannedCount} of {KernelTypes.Length} kernel types — the fitness test ran vacuously");
        Assert.True(result.Violations.Count == 0,
            $"The following {result.Violations.Count} kernel type(s) lack a test class:\n" +
            string.Join("\n", result.Violations));
    }

    private static CoverageResult CollectMissingTestClasses(
        Type[] kernelTypes, HashSet<string> testClassNames, Assembly commonAssembly)
    {
        var violations = new List<string>();

        foreach (var kernelType in kernelTypes)
        {
            if (kernelType.Assembly != commonAssembly)
            {
                violations.Add($"  {kernelType.FullName} — not defined in the JiApp.Common assembly");
                continue;
            }

            var expectedTestClass = TestClassNameFor(kernelType);
            if (!testClassNames.Contains(expectedTestClass))
                violations.Add($"  {kernelType.FullName} — no '{expectedTestClass}' in JiApp.Common.Tests");
        }

        return new CoverageResult(violations, kernelTypes.Length);
    }

    private static string TestClassNameFor(Type kernelType) => kernelType.Name.Split('`')[0] + "Tests";

    private sealed record CoverageResult(List<string> Violations, int ScannedCount);
}
