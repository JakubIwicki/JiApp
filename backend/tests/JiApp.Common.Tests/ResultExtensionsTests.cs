using JiApp.Common.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace JiApp.Common.Tests;

public sealed class ResultExtensionsTests
{
    [Fact]
    public async Task Succeeds()
    {
        var result = Result<int>.Success(42);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.BodyText.Should().Be("42");
    }

    [Fact]
    public async Task Returns404_WhenCategoryNotFound()
    {
        var result = Result<int>.Failure("Not found", ResultCategories.NotFound);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Returns404_WhenAccessDenied()
    {
        // G3.2 policy: hide the resource's existence from an unauthorized caller.
        var result = Result<int>.Failure("Access denied", ResultCategories.AccessDenied);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task Returns400_WhenValidation()
    {
        var result = Result<int>.Failure("Invalid input", ResultCategories.Validation);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Returns409_WhenConflict()
    {
        var result = Result<int>.Failure("Conflict", ResultCategories.Conflict);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
    }

    [Fact]
    public async Task Returns502_WhenBadGateway()
    {
        var result = Result<int>.Failure("Upstream failed", ResultCategories.BadGateway);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status502BadGateway);
    }

    [Fact]
    public async Task Returns404WithUnknownErrorMessage_WhenErrorIsNull()
    {
        var result = Result<int>.Failure(null!, ResultCategories.NotFound);

        var response = await Act(result);

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        response.BodyText.Should().Contain(ApiErrorResponse.UnknownErrorMessage);
    }

    private static async Task<HttpResponseSnapshot> Act<T>(Result<T> result)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<JsonOptions>(_ => { });
        httpContext.RequestServices = services.BuildServiceProvider();

        await result.ToHttp().ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body);
        var body = await reader.ReadToEndAsync();
        return new HttpResponseSnapshot(httpContext.Response.StatusCode, body);
    }

    private sealed record HttpResponseSnapshot(int StatusCode, string BodyText);
}
