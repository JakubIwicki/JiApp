using Google.Apis.Http;

namespace JiApp.YtDownloader.Services;

/// <summary>
/// Supplies the YouTube API's HTTP client with <see cref="ConfigurableMessageHandler.NumTries"/> = 1
/// so Google.Apis' internal retry loop (408/429/500/503) is disabled and the app's owned Polly retry
/// policy is the single retry owner. Otherwise a sustained 429 stacks Google's 3 internal tries on
/// top of Polly's 3 retries — up to ~9-12 upstream calls that worsen the rate-limit condition.
/// </summary>
public sealed class SingleTryGoogleHttpClientFactory : Google.Apis.Http.IHttpClientFactory
{
    public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args) =>
        new(new ConfigurableMessageHandler(new HttpClientHandler()) { NumTries = 1 });
}
