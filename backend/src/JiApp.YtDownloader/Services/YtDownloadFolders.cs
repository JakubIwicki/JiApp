namespace JiApp.YtDownloader.Services;

/// <summary>
/// The single owner of the per-user output-folder convention. The worker writes
/// downloads there; the job store reaps stuck jobs' partial files from there —
/// both must agree on the folder shape.
/// </summary>
internal static class YtDownloadFolders
{
    public const string UserOutputPrefix = "YtMp3_";

    public static string ForUser(string? baseDirectory, long userId) =>
        Path.Combine(baseDirectory ?? "/tmp", $"{UserOutputPrefix}{userId}");
}
