namespace JiApp.YtDownloader.Features.GetDownloadLink;

[Serializable]
public sealed record DownloadResponse(string TempId, string DownloadUrl)
{
    public static DownloadResponse WithUrl(string tempId, string scheme, string host)
        => new(tempId, $"{scheme}://{host}/api/v1/yt/downloads/mp3/file/{tempId}");
}