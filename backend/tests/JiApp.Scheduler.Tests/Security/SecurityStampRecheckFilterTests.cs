using JiApp.Common.Abstractions;
using JiApp.Common.Middleware;
using JiApp.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace JiApp.Scheduler.Tests.Security;

public sealed class SecurityStampRecheckFilterTests
{
	private sealed class FakeValidator(StampValidationResult result) : ISecurityStampValidator
	{
		public Task<StampValidationResult> ValidateCurrentAsync(CancellationToken ct = default)
			=> Task.FromResult(result);
	}

	[Fact]
	public async Task InvokeAsync_ValidStamp_CallsNextAndReturnsItsResult()
	{
		var filter = new SecurityStampRecheckFilter(new FakeValidator(StampValidationResult.Valid));
		var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
		var nextCalled = false;
		ValueTask<object?> ExpectedNext(EndpointFilterInvocationContext ctx)
		{
			nextCalled = true;
			return ValueTask.FromResult<object?>(Results.Ok());
		}

		var result = await filter.InvokeAsync(context, ExpectedNext);

		nextCalled.Should().BeTrue();
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task InvokeAsync_RevokedStamp_Returns401AndDoesNotCallNext()
	{
		var filter = new SecurityStampRecheckFilter(new FakeValidator(StampValidationResult.Revoked));
		var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
		var nextCalled = false;
		ValueTask<object?> ExpectedNext(EndpointFilterInvocationContext ctx)
		{
			nextCalled = true;
			return ValueTask.FromResult<object?>(Results.Ok());
		}

		var result = await filter.InvokeAsync(context, ExpectedNext);

		nextCalled.Should().BeFalse();
		var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiErrorResponse>>().Subject;
		jsonResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
		jsonResult.Value!.Error.Should().Be("Token has been revoked");
	}

	[Fact]
	public async Task InvokeAsync_UnavailableStamp_Returns503AndDoesNotCallNext()
	{
		var filter = new SecurityStampRecheckFilter(new FakeValidator(StampValidationResult.Unavailable));
		var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
		var nextCalled = false;
		ValueTask<object?> ExpectedNext(EndpointFilterInvocationContext ctx)
		{
			nextCalled = true;
			return ValueTask.FromResult<object?>(Results.Ok());
		}

		var result = await filter.InvokeAsync(context, ExpectedNext);

		nextCalled.Should().BeFalse();
		var jsonResult = result.Should().BeOfType<JsonHttpResult<ApiErrorResponse>>().Subject;
		jsonResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
		jsonResult.Value!.Error.Should().Be("Authorization service unavailable");
	}
}
