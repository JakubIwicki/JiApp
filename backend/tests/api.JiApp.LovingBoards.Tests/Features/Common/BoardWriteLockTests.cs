using api.JiApp.LovingBoards.Common;

namespace api.JiApp.LovingBoards.Tests.Features.Common;

public sealed class BoardWriteLockTests
{
    [Fact]
    public async Task Acquire_SameBoardId_MutuallyExclusive()
    {
        var @lock = new BoardWriteLock();
        var entered = false;
        var disposed = false;

        var first = await @lock.AcquireAsync(1L, CancellationToken.None);
        try
        {
            var secondTask = Task.Run(async () =>
            {
                using var second = await @lock.AcquireAsync(1L, CancellationToken.None);
                entered = true;
            });

            await Task.Delay(100);
            entered.Should().BeFalse("second acquire should block until first releases");

            first.Dispose();
            disposed = true;
            await secondTask;

            entered.Should().BeTrue("second acquire should complete after first releases");
        }
        finally
        {
            if (!disposed)
                first.Dispose();
        }
    }

    [Fact]
    public async Task Acquire_DifferentBoardIds_Independent()
    {
        var @lock = new BoardWriteLock();

        using var first = await @lock.AcquireAsync(1L, CancellationToken.None);
        // Should not block — different board
        using var second = await @lock.AcquireAsync(2L, CancellationToken.None);

        // Both acquired simultaneously = no deadlock, no blocking
    }

    [Fact]
    public async Task Acquire_RespectsCancellationToken()
    {
        var @lock = new BoardWriteLock();

        using var holder = await @lock.AcquireAsync(1L, CancellationToken.None);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var acquireTask = @lock.AcquireAsync(1L, cts.Token);
        await acquireTask.Awaiting(t => t).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Acquire_AfterRelease_AllowsReEntry()
    {
        var @lock = new BoardWriteLock();

        using (await @lock.AcquireAsync(1L, CancellationToken.None))
        {
            // held briefly
        }

        // Same boardId should be acquirable again after release
        using var second = await @lock.AcquireAsync(1L, CancellationToken.None);
        // No deadlock = pass
    }
}
