using FluentValidation;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Clients.UpdateClient;

public sealed class UpdateClientValidator : AbstractValidator<UpdateClientRequest>
{
    public UpdateClientValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200)
            .Must(name => !name.Contains('<') && !name.Contains('>'))
            .WithMessage("Name must not contain HTML tags.");
        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .Matches(ClientValidationConstants.PhoneRegexPattern).When(x => !string.IsNullOrEmpty(x.Phone));
        RuleFor(x => x.Notes).MaximumLength(1000).When(x => x.Notes is not null);
    }
}
