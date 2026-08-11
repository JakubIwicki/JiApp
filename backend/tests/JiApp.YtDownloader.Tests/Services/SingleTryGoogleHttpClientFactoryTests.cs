using Google.Apis.Http;
using JiApp.YtDownloader.Services;

namespace JiApp.YtDownloader.Tests.Services;

public sealed class SingleTryGoogleHttpClientFactoryTests
{
    [Fact]
    public void CreateHttpClient_ReturnsClient_WithSingleTryHandler()
    {
        var factory = new SingleTryGoogleHttpClientFactory();

        var client = factory.CreateHttpClient(new CreateHttpClientArgs());

        client.MessageHandler.NumTries.Should().Be(1);
    }
}
