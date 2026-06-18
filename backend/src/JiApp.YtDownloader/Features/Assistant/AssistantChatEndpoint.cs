using System.Text.Json;
using FluentValidation;
using JiApp.Common.Abstractions;
using JiApp.YtDownloader.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace JiApp.YtDownloader.Features.Assistant;

public static class AssistantChatEndpoint
{
    private const string EnglishLanguage = "en";
    private const string PolishLanguage = "pl";

    private static readonly JsonSerializerOptions SseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static IEndpointRouteBuilder MapAssistantChat(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/assistant/chat", async (
                AssistantChatRequest request,
                IValidator<AssistantChatRequest> validator,
                AssistantChatHandler handler,
                AssistantChatOrchestrator orchestrator,
                AssistantStreamGate streamGate,
                ICurrentUserService currentUser,
                Settings settings,
                HttpContext httpContext) =>
            {
                var validationResult = await validator.ValidateAsync(request, httpContext.RequestAborted);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToArray();
                    return Results.Extensions.ValidationError(errors);
                }

                var userId = currentUser.UserId;
                var language = NormalizeLanguage(request.Language);
                var dailyLimit = settings.Assistant?.DailyMessageLimitPerUser ?? 30;

                var preCheck = await handler.PreCheckAsync(userId, dailyLimit, httpContext.RequestAborted);
                if (preCheck == AssistantChatPreCheck.QuotaExceeded)
                    return QuotaExceeded(language);
                if (preCheck == AssistantChatPreCheck.NotConfigured)
                    return NotConfigured(language);

                if (!streamGate.TryEnter())
                    return Busy(language);

                try
                {
                    await StreamSseAsync(httpContext, orchestrator, request.Messages, language, userId);
                }
                finally
                {
                    streamGate.Release();
                }

                return Results.Empty;
            })
            .WithTags(SwaggerConstants.Tags.Assistant)
            .WithSummary("Stream a chat turn with the music assistant (SSE)")
            .ProducesValidationProblem()
            .Produces<ApiErrorResponse>(StatusCodes.Status429TooManyRequests)
            .Produces<ApiErrorResponse>(StatusCodes.Status503ServiceUnavailable)
            .RequireAuthorization();

        return endpoints;
    }

    private static string NormalizeLanguage(string? language) =>
        string.Equals(language, EnglishLanguage, StringComparison.OrdinalIgnoreCase)
            ? EnglishLanguage
            : PolishLanguage;

    private static IResult QuotaExceeded(string language)
    {
        var message = language == EnglishLanguage
            ? "You've reached today's assistant message limit. Please try again tomorrow."
            : "Osiągnięto dzienny limit wiadomości do asystenta. Spróbuj ponownie jutro.";
        return Results.Json(
            new ApiErrorResponse(Error: message),
            ApiErrorResponse.JsonOptions,
            statusCode: StatusCodes.Status429TooManyRequests);
    }

    private static IResult NotConfigured(string language)
    {
        var message = language == EnglishLanguage
            ? "The assistant is temporarily unavailable."
            : "Asystent jest chwilowo niedostępny.";
        return Results.Json(
            new ApiErrorResponse(Error: message),
            ApiErrorResponse.JsonOptions,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static IResult Busy(string language)
    {
        var message = language == EnglishLanguage
            ? "The assistant is busy with another request. Please try again in a moment."
            : "Asystent jest zajęty innym zapytaniem. Spróbuj ponownie za chwilę.";
        return Results.Json(
            new ApiErrorResponse(Error: message),
            ApiErrorResponse.JsonOptions,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task StreamSseAsync(
        HttpContext httpContext,
        AssistantChatOrchestrator orchestrator,
        IReadOnlyList<ChatMessageDto> messages,
        string language,
        long userId)
    {
        var response = httpContext.Response;
        response.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers["X-Accel-Buffering"] = "no";

        httpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        var ct = httpContext.RequestAborted;

        await response.WriteAsync(": keep-alive\n\n", ct);
        await response.Body.FlushAsync(ct);

        await foreach (var ev in orchestrator.StreamAsync(messages, language, userId, ct))
        {
            var json = JsonSerializer.Serialize(ev.Data, SseJsonOptions);
            await response.WriteAsync($"event: {ev.Event}\n", ct);
            await response.WriteAsync($"data: {json}\n\n", ct);
            await response.Body.FlushAsync(ct);
        }
    }
}
