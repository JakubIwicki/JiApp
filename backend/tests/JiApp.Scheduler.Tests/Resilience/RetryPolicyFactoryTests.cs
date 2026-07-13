using JiApp.Common.Resilience;
using Microsoft.EntityFrameworkCore;

namespace JiApp.Scheduler.Tests.Resilience;

public sealed class RetryPolicyFactoryTests
{
	private readonly IRetryPolicyFactory _factory = new RetryPolicyFactory();

	[Fact]
	public async Task RetryOnDbConflict_RetriesOnDbUpdateException_ThenPropagates()
	{
		var policy = _factory.RetryOnDbConflict(retries: 2, delay: TimeSpan.Zero);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			throw new DbUpdateException("concurrent write conflict",
				(Exception?)new InvalidOperationException("inner"));
		}

		var ex = await Record.ExceptionAsync(() => policy.ExecuteAsync(Action, CancellationToken.None).AsTask());

		ex.Should().BeOfType<DbUpdateException>();
		attempts.Should().Be(3); // initial + 2 retries
	}

	[Fact]
	public async Task RetryOnDbConflict_SucceedsOnSecondAttempt()
	{
		var policy = _factory.RetryOnDbConflict(retries: 2, delay: TimeSpan.Zero);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			if (attempts < 2)
				throw new DbUpdateException("fail",
					(Exception?)new InvalidOperationException("inner"));
			return 42;
		}

		var result = await policy.ExecuteAsync(Action, CancellationToken.None);

		result.Should().Be(42);
		attempts.Should().Be(2);
	}

	[Fact]
	public async Task RetryOnDbConflict_NonMatchedException_PassesStraightThrough()
	{
		var policy = _factory.RetryOnDbConflict(retries: 5, delay: TimeSpan.Zero);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			throw new InvalidOperationException("not a DB exception");
		}

		var ex = await Record.ExceptionAsync(() => policy.ExecuteAsync(Action, CancellationToken.None).AsTask());

		ex.Should().BeOfType<InvalidOperationException>();
		attempts.Should().Be(1); // no retry on unmatched exception
	}

	[Fact]
	public async Task RetryOnTransientHttp_RetriesOnHttpRequestException_ThenPropagates()
	{
		var policy = _factory.RetryOnTransientHttp_WithExponentialBackoff(retries: 2);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			throw new HttpRequestException("connection refused");
		}

		var ex = await Record.ExceptionAsync(() => policy.ExecuteAsync(Action, CancellationToken.None).AsTask());

		ex.Should().BeOfType<HttpRequestException>();
		attempts.Should().Be(3); // initial + 2 retries
	}

	[Fact]
	public async Task RetryOnTransientHttp_RetriesOnTaskCanceledException_ThenPropagates()
	{
		var policy = _factory.RetryOnTransientHttp_WithExponentialBackoff(retries: 1);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			throw new TaskCanceledException();
		}

		var ex = await Record.ExceptionAsync(() => policy.ExecuteAsync(Action, CancellationToken.None).AsTask());

		ex.Should().BeOfType<TaskCanceledException>();
		attempts.Should().Be(2); // initial + 1 retry
	}

	[Fact]
	public async Task RetryOnTransientHttp_NonMatchedException_PassesStraightThrough()
	{
		var policy = _factory.RetryOnTransientHttp_WithExponentialBackoff(retries: 3);
		var attempts = 0;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			throw new ArgumentException("bad argument");
		}

		var ex = await Record.ExceptionAsync(() => policy.ExecuteAsync(Action, CancellationToken.None).AsTask());

		ex.Should().BeOfType<ArgumentException>();
		attempts.Should().Be(1); // no retry on unmatched exception
	}

	[Fact]
	public async Task RetryOnDbConflict_UsesRequestedDelay()
	{
		var delay = TimeSpan.FromMilliseconds(50);
		var policy = _factory.RetryOnDbConflict(retries: 1, delay: delay);
		var attempts = 0;
		var start = Environment.TickCount64;

		async ValueTask<int> Action(CancellationToken ct)
		{
			attempts++;
			if (attempts == 1)
				throw new DbUpdateException("fail",
					(Exception?)new InvalidOperationException("inner"));
			return 1;
		}

		await policy.ExecuteAsync(Action, CancellationToken.None);

		var elapsed = Environment.TickCount64 - start;
		attempts.Should().Be(2);
		elapsed.Should().BeGreaterThanOrEqualTo((long)(delay.TotalMilliseconds * 0.8));
	}
}
