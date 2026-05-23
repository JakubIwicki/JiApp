using System.Collections.Generic;
using System.Threading.Tasks;

namespace JiApp.YtApi;

public interface IYoutubeClient
{
    Task<IReadOnlyList<YoutubeVideo>> SearchVideosAsync(string query, int maxResults = 10);
    Task<YoutubeClientResponse> DownloadVideoAsync(string videoId, string outputPath);
}