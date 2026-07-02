using System.Net;
using JiApp.Common.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JiApp.Scheduler.Tests.Security;

public sealed class RemoteSecurityStampValidatorTests
{
	private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
	{
		public Exception? ThrowException { get; set; }

		protected override Task<HttpResponseMessage> SendAsync(
			HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (ThrowException is not null)
				throw ThrowException;

			return Task.FromResult(new HttpResponseMessage(statusCode));
		}
	}

	private static ILogger<T> NullLogger<T>() => NullLoggerFactory.Instance.CreateLogger<T>();

	private static IHttpContextAccessor CreateHttpContextAccessor(string? authHeader = "Bearer testtoken")
	{
		var httpContext = new DefaultHttpContext();
		if (authHeader is not null)
			httpContext.Request.Headers.Authorization = authHeader;
		return new HttpContextAccessor { HttpContext = httpContext };
	}

	[Fact]
	public async Task ValidateCurrentAsync_204Response_ReturnsValid()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor();
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Valid);
	}

	[Fact]
	public async Task ValidateCurrentAsync_401Response_ReturnsRevoked()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.Unauthorized);
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor();
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Revoked);
	}

	[Fact]
	public async Task ValidateCurrentAsync_500Response_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError);
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor();
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Unavailable);
	}

	[Fact]
	public async Task ValidateCurrentAsync_HttpRequestException_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK)
		{
			ThrowException = new HttpRequestException("Connection refused")
		};
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor();
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Unavailable);
	}

	[Fact]
	public async Task ValidateCurrentAsync_NoAuthorizationHeader_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor(authHeader: null);
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Unavailable);
	}

	[Fact]
	public async Task ValidateCurrentAsync_TaskCanceledException_Timeout_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.OK)
		{
			ThrowException = new TaskCanceledException()
		};
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = CreateHttpContextAccessor();
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Unavailable);
	}

	[Fact]
	public async Task ValidateCurrentAsync_NoHttpContext_ReturnsUnavailable()
	{
		var handler = new StubHttpMessageHandler(HttpStatusCode.NoContent);
		using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://identity:6701") };
		var accessor = new HttpContextAccessor { HttpContext = null! }; // no HttpContext
		var validator = new RemoteSecurityStampValidator(httpClient, accessor, NullLogger<RemoteSecurityStampValidator>());

		var result = await validator.ValidateCurrentAsync();

		result.Should().Be(StampValidationResult.Unavailable);
	}
}
