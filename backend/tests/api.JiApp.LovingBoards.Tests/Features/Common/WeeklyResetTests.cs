using api.JiApp.LovingBoards.Features.Common;

namespace api.JiApp.LovingBoards.Tests.Features.Common;

public sealed class WeeklyResetTests
{
    [Fact]
    public void IsResetDue_WhenLastResetIsNull_ReturnsTrue()
    {
        WeeklyReset.IsResetDue(null, DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsResetDue_WhenLastResetIsBeforeCurrentWeek_ReturnsTrue()
    {
        // Wednesday 2026-07-01; Monday of that week is 2026-06-29
        var now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var lastReset = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

        WeeklyReset.IsResetDue(lastReset, now).Should().BeTrue();
    }

    [Fact]
    public void IsResetDue_WhenLastResetIsWithinCurrentWeek_ReturnsFalse()
    {
        var now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc); // Wednesday
        var lastReset = new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc); // Monday, same week

        WeeklyReset.IsResetDue(lastReset, now).Should().BeFalse();
    }

    [Fact]
    public void IsResetDue_WhenLastResetIsAtMondayMidnightOfCurrentWeek_ReturnsFalse()
    {
        var now = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc); // Wednesday
        var lastReset = new DateTime(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc); // Monday 00:00, same week

        WeeklyReset.IsResetDue(lastReset, now).Should().BeFalse();
    }

    [Fact]
    public void IsResetDue_WhenLastResetIsSundayBeforeMonday_ReturnsTrue()
    {
        var now = new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc); // Monday
        var lastReset = new DateTime(2026, 6, 28, 23, 59, 59, DateTimeKind.Utc); // Sunday 23:59, prior week

        WeeklyReset.IsResetDue(lastReset, now).Should().BeTrue();
    }

    [Fact]
    public void ResetRecurring_FlipsCompletedAndRemovedToNeeded()
    {
        var now = DateTime.UtcNow;
        var items = new List<BoardItem>
        {
            new() { IsRecurring = true, Status = BoardItemStatus.Completed, CompletedByUserId = 2L },
            new() { IsRecurring = true, Status = BoardItemStatus.Removed, RemovedAt = now.AddHours(-1) },
        };

        var count = WeeklyReset.ResetRecurring(items, now);

        count.Should().Be(2);
        items[0].Status.Should().Be(BoardItemStatus.Needed);
        items[0].CompletedByUserId.Should().BeNull();
        items[0].RemovedAt.Should().BeNull();
        items[0].UpdatedAt.Should().Be(now);
        items[1].Status.Should().Be(BoardItemStatus.Needed);
        items[1].CompletedByUserId.Should().BeNull();
        items[1].RemovedAt.Should().BeNull();
        items[1].UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void ResetRecurring_IgnoresNonRecurringItems()
    {
        var now = DateTime.UtcNow;
        var items = new List<BoardItem>
        {
            new() { IsRecurring = false, Status = BoardItemStatus.Completed, CompletedByUserId = 2L },
            new() { IsRecurring = false, Status = BoardItemStatus.Removed },
        };

        var count = WeeklyReset.ResetRecurring(items, now);

        count.Should().Be(0);
        items[0].Status.Should().Be(BoardItemStatus.Completed);
        items[1].Status.Should().Be(BoardItemStatus.Removed);
    }

    [Fact]
    public void ResetRecurring_IgnoresRecurringNeededItems()
    {
        var now = DateTime.UtcNow;
        var neededItem = new BoardItem { IsRecurring = true, Status = BoardItemStatus.Needed };
        var items = new List<BoardItem> { neededItem };

        var count = WeeklyReset.ResetRecurring(items, now);

        count.Should().Be(0);
        neededItem.Status.Should().Be(BoardItemStatus.Needed);
    }

    [Fact]
    public void ResetRecurring_ReturnsCorrectCountForMixedItems()
    {
        var now = DateTime.UtcNow;
        var items = new List<BoardItem>
        {
            new() { IsRecurring = true, Status = BoardItemStatus.Completed },
            new() { IsRecurring = true, Status = BoardItemStatus.Removed },
            new() { IsRecurring = false, Status = BoardItemStatus.Completed },
            new() { IsRecurring = true, Status = BoardItemStatus.Needed },
        };

        var count = WeeklyReset.ResetRecurring(items, now);

        count.Should().Be(2);
    }
}
