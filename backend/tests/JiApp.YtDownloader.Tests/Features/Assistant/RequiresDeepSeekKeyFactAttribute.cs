namespace JiApp.YtDownloader.Tests.Features.Assistant;

/// <summary>
/// A <see cref="FactAttribute"/> that skips the test when the <c>DEEPSEEK_API_KEY</c>
/// environment variable is absent, so CI stays green without a real key. To run these
/// tests locally, export the key first:
/// <code>DEEPSEEK_API_KEY=sk-... dotnet test --filter Category=DeepSeekIntegration</code>
/// </summary>
public sealed class RequiresDeepSeekKeyFactAttribute : FactAttribute
{
    public const string EnvVarName = "DEEPSEEK_API_KEY";

    public RequiresDeepSeekKeyFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvVarName)))
            Skip = $"Skipped: set the {EnvVarName} environment variable to run DeepSeek integration tests.";
    }
}
