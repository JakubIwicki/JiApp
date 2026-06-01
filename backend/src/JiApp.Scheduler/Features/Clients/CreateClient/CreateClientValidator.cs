using FluentValidation;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Clients.CreateClient;

public sealed class CreateClientValidator : AbstractValidator<CreateClientRequest>
{
    public CreateClientValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .Matches(ClientValidationConstants.PhoneRegexPattern).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}