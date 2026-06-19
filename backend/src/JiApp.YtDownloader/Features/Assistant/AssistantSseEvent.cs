namespace JiApp.YtDownloader.Features.Assistant;

public sealed record AssistantSseEvent(string Event, object Data);

public static class AssistantSseEventNames
{
    public const string TextDelta = "text-delta";
    public const string ToolStep = "tool-step";
    public const string SearchResults = "search-results";
    public const string DownloadOffer = "download-offer";
    public const string Done = "done";
}

public static class AssistantToolStepStatus
{
    public const string Running = "running";
    public const string Done = "done";
}

public static class AssistantDoneReasons
{
    public const string Complete = "complete";
    public const string MaxIterations = "max_iterations";
    public const string Error = "error";
}
