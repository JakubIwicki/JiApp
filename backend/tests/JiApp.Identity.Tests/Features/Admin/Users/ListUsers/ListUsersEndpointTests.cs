using JiApp.Common.Abstractions;
using JiApp.Identity.Features.Admin.Users.ListUsers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JiApp.Identity.Tests.Features.Admin.Users.ListUsers;

public sealed class ListUsersEndpointTests
{
    [Fact]
    public async Task ToHttpResult_WhenSuccess_ReturnsOk_WithUsers()
    {
        var response = new ListUsersResponse([], TotalCount: 0);
        var context = NewContext();

        await ListUsersEndpoint.ToHttpResult(Result<ListUsersResponse>.Success(response)).ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task ToHttpResult_WhenFailure_ReturnsServerError_NotTheValue()
    {
        // The handler always succeeds today, but the guard must hold: a failure
        // result must never dereference result.Value — the pre-fix code did exactly
        // that and would have thrown NullReferenceException.
        var context = NewContext();

        await ListUsersEndpoint.ToHttpResult(
            Result<ListUsersResponse>.Failure("unexpected")).ExecuteAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
    }

    private static DefaultHttpContext NewContext() =>
        new()
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
}
