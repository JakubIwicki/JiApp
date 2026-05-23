namespace JiApp.YtApi;

 public sealed record YoutubeVideo(string VideoId, string Title, string Description, string ImageUrl, string ChannelTitle)
{
    public string VideoUrl => $"https://www.youtube.com/watch?v={VideoId}";
}
