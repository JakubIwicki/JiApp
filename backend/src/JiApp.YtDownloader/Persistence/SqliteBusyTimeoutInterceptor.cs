using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JiApp.YtDownloader.Persistence;

/// <summary>
/// SQLite serializes writers; when the download worker and request handlers write at the
/// same time, a second writer can hit "database is locked" unless the connection waits for
/// the lock. PRAGMA busy_timeout is per-connection, so it must run on every open.
/// </summary>
public sealed class SqliteBusyTimeoutInterceptor : DbConnectionInterceptor
{
    private const int BusyTimeoutMilliseconds = 30_000;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA busy_timeout = {BusyTimeoutMilliseconds};";
        command.ExecuteNonQuery();
        base.ConnectionOpened(connection, eventData);
    }
}
