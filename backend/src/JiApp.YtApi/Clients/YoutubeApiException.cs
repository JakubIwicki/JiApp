namespace JiApp.YtApi.Clients;

public sealed class YoutubeApiException : Exception
{
    public YoutubeApiException(string message) : base(message) { }
    public YoutubeApiException(string message, Exception innerException) : base(message, innerException) { }
}
