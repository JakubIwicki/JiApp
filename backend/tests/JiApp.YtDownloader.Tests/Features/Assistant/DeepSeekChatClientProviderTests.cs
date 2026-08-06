using JiApp.Common.Resilience;
using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class DeepSeekChatClientProviderTests
{
    private static readonly IRetryPolicyFactory RetryPolicy = new RetryPolicyFactory(TimeProvider.System);

    [Fact]
    public void Provider_WithAbsentDeepSeekSection_IsNotConfigured()
    {
        var provider = new DeepSeekChatClientProvider(new Settings(), RetryPolicy);

        provider.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Provider_WithBlankApiKey_IsNotConfigured(string? apiKey)
    {
        var settings = new Settings
        {
            DeepSeek = new Settings.DeepSeekSettings { ApiKey = apiKey }
        };

        var provider = new DeepSeekChatClientProvider(settings, RetryPolicy);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Provider_WhenNotConfigured_ThrowsOnClientAccess()
    {
        var provider = new DeepSeekChatClientProvider(new Settings(), RetryPolicy);

        var act = () => provider.Client;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Provider_WithApiKey_IsConfiguredAndBuildsClient()
    {
        var settings = new Settings
        {
            DeepSeek = new Settings.DeepSeekSettings { ApiKey = "sk-test-key-not-real" }
        };

        var provider = new DeepSeekChatClientProvider(settings, RetryPolicy);

        provider.IsConfigured.Should().BeTrue();
        provider.Client.Should().NotBeNull();
    }
}
