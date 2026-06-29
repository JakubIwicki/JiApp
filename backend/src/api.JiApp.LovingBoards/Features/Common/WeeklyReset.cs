using api.JiApp.LovingBoards.Domain;

namespace api.JiApp.LovingBoards.Features.Common;

internal static class WeeklyReset
{
    internal static bool IsResetDue(DateTime? lastResetUtc, DateTime nowUtc)
    {
        if (lastResetUtc is null)
            return true;

        var monday = GetMondayOfWeek(nowUtc);
        return lastResetUtc.Value < monday;
    }

    internal static int ResetRecurring(IEnumerable<BoardItem> items, DateTime nowUtc)
    {
        var count = 0;
        foreach (var item in items)
        {
            if (!item.IsRecurring)
                continue;

            if (item.Status is not (BoardItemStatus.Completed or BoardItemStatus.Removed))
                continue;

            item.Status = BoardItemStatus.Needed;
            item.CompletedByUserId = null;
            item.RemovedAt = null;
            item.UpdatedAt = nowUtc;
            count++;
        }
        return count;
    }

    private static DateTime GetMondayOfWeek(DateTime dt)
    {
        // DayOfWeek: Sunday=0, Monday=1, …, Saturday=6.
        // Days since Monday: Monday → 0, Tuesday → 1, …, Sunday → 6.
        var daysSinceMonday = ((int)dt.DayOfWeek + 6) % 7;
        return dt.Date.AddDays(-daysSinceMonday);
    }
}
