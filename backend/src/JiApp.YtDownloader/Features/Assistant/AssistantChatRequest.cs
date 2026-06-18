namespace JiApp.YtDownloader.Features.Assistant;

public sealed record AssistantChatRequest(
    IReadOnlyList<ChatMessageDto> Messages,
    string? Language);

public sealed record ChatMessageDto(string Role, string Content);

public static class ChatMessageRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
}
