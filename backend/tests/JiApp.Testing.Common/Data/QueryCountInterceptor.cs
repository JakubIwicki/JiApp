using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace JiApp.Testing.Common.Data;

/// <summary>
/// Counts result-returning database commands (SELECTs) so a test can pin the
/// number of round trips a query path performs. N+1 regressions surface as a
/// count that scales with the data size instead of staying flat.
/// </summary>
public sealed class QueryCountInterceptor : DbCommandInterceptor
{
    public int Count { get; private set; }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count++;
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    public void Reset() => Count = 0;
}
