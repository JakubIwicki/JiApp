using FluentValidation;

namespace JiApp.YtDownloader.Features.Assistant;

public sealed class AssistantChatValidator : AbstractValidator<AssistantChatRequest>
{
    private const int MaxContentLength = 4000;

    private static readonly string[] AllowedRoles =
        [ChatMessageRoles.User, ChatMessageRoles.Assistant];

    public AssistantChatValidator()
    {
        RuleFor(x => x.Messages)
            .NotEmpty()
            .WithMessage("At least one message is required.");

        RuleFor(x => x.Messages)
            .Must(messages => messages.Count > 0 && messages[^1].Role == ChatMessageRoles.User)
            .WithMessage("The last message must be from the user.")
            .When(x => x.Messages is { Count: > 0 });

        RuleForEach(x => x.Messages).ChildRules(message =>
        {
            message.RuleFor(m => m.Role)
                .Must(role => AllowedRoles.Contains(role))
                .WithMessage("Each message role must be 'user' or 'assistant'.");

            message.RuleFor(m => m.Content)
                .NotEmpty()
                .MaximumLength(MaxContentLength);
        });
    }
}
