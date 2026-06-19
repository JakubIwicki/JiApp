using JiApp.YtDownloader.Configuration;
using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class DeepSeekChatClientProviderTests
{
    [Fact]
    public void Provider_is_not_configured_when_deepseek_section_is_absent()
    {
        var provider = new DeepSeekChatClientProvider(new Settings());

        provider.IsConfigured.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Provider_is_not_configured_when_api_key_is_blank(string? apiKey)
    {
        var settings = new Settings
        {
            DeepSeek = new Settings.DeepSeekSettings { ApiKey = apiKey }
        };

        var provider = new DeepSeekChatClientProvider(settings);

        provider.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public void Accessing_client_when_not_configured_throws()
    {
        var provider = new DeepSeekChatClientProvider(new Settings());

        var act = () => provider.Client;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Provider_is_configured_and_builds_client_when_api_key_present()
    {
        var settings = new Settings
        {
            DeepSeek = new Settings.DeepSeekSettings { ApiKey = "sk-test-key-not-real" }
        };

        var provider = new DeepSeekChatClientProvider(settings);

        provider.IsConfigured.Should().BeTrue();
        provider.Client.Should().NotBeNull();
    }
}
