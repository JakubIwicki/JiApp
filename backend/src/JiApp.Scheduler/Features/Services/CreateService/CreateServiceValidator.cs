using FluentValidation;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Services.CreateService;

public sealed class CreateServiceValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50)
            .Must(Validators.BeValidServiceCategory).WithMessage("Invalid service category");
        RuleFor(x => x.BaseDuration).GreaterThan(0);
        RuleFor(x => x.BasePrice.Amount).GreaterThanOrEqualTo(0);
    }
}