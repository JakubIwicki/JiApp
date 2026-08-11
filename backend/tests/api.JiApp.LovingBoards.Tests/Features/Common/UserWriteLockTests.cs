using api.JiApp.LovingBoards.Common;

namespace api.JiApp.LovingBoards.Tests.Features.Common;

public sealed class UserWriteLockTests
{
    [Fact]
    public async Task Acquire_SameUserId_MutuallyExclusive()
    {
        var @lock = new UserWriteLock();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var disposed = false;

        var first = await @lock.AcquireAsync(1L, CancellationToken.None);
        try
        {
            var secondTask = Task.Run(async () =>
            {
                using var second = await @lock.AcquireAsync(1L, CancellationToken.None);
                entered.TrySetResult();
            });

            // Bounded wait: while the first lock is held the second acquire must stay blocked.
            var completed = await Task.WhenAny(entered.Task, Task.Delay(TimeSpan.FromMilliseconds(100)));
            completed.Should().NotBe(entered.Task, "second acquire should block until first releases");

            first.Dispose();
            disposed = true;

            // Bounded wait: after release the second acquire must complete — fails fast, never hangs.
            await secondTask.WaitAsync(TimeSpan.FromSeconds(2));
        }
        finally
        {
            if (!disposed)
                first.Dispose();
        }
    }

    [Fact]
    public async Task Acquire_DifferentUserIds_Independent()
    {
        var @lock = new UserWriteLock();

        using var first = await @lock.AcquireAsync(1L, CancellationToken.None);

        // Bounded wait: an acquire for a different user must never block on another user's lock.
        var secondTask = @lock.AcquireAsync(2L, CancellationToken.None);
        var completed = await Task.WhenAny(secondTask, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().Be(secondTask, "acquiring a different user's lock must not block");

        using var second = await secondTask;
    }

    [Fact]
    public async Task Acquire_RespectsCancellationToken()
    {
        var @lock = new UserWriteLock();

        using var holder = await @lock.AcquireAsync(1L, CancellationToken.None);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        var acquireTask = @lock.AcquireAsync(1L, cts.Token);
        await acquireTask.Awaiting(t => t).Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Acquire_AfterRelease_AllowsReEntry()
    {
        var @lock = new UserWriteLock();

        using (await @lock.AcquireAsync(1L, CancellationToken.None))
        {
            // held briefly
        }

        // Bounded wait: re-acquiring the same user lock after release must complete.
        var reacquire = @lock.AcquireAsync(1L, CancellationToken.None);
        var completed = await Task.WhenAny(reacquire, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().Be(reacquire, "re-acquiring the same user lock after release must not block");

        using var second = await reacquire;
    }
}
