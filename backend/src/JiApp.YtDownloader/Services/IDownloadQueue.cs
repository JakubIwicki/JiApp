using JiApp.YtDownloader.Domain;

namespace JiApp.YtDownloader.Services;

/// <summary>
/// The database-as-queue surface the download worker drives. It is internal because these
/// operations are the worker's own claim/scan machinery, not job-state the HTTP handlers
/// are entitled to. <see cref="DownloadJobStore"/> implements both this and the public
/// <see cref="IDownloadJobStore"/>.
/// </summary>
internal interface IDownloadQueue
{
    /// <summary>
    /// Returns every row stuck in Processing (an orphan from a crashed/killed worker) to
    /// Queued, and returns the number of rows reset. NOTE: this resets ALL Processing rows,
    /// which is only safe while at most one worker instance exists — a multi-instance
    /// deployment would need a host/lease column to leave another live worker's in-flight
    /// rows alone.
    /// </summary>
    int ResetOrphanedProcessing();

    /// <summary>
    /// The queue scan: rows that are ready to run — freshly Queued, or Failed rows whose
    /// retry backoff (<see cref="DownloadCommand.NextAttemptAt"/>) has elapsed. Ordered
    /// oldest-first, capped at <paramref name="count"/>.
    /// </summary>
    IReadOnlyList<string> GetEligibleTempIds(int count);

    /// <summary>
    /// Atomically moves a single eligible row (Queued, or Failed with its backoff elapsed)
    /// to Processing. Returns true only for the worker that won the claim — a second worker
    /// touching the same row gets false.
    /// </summary>
    bool ClaimEligible(string tempId, long userId);
}
