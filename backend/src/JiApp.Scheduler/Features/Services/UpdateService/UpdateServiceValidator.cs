using FluentValidation;
using JiApp.Scheduler.Features.Common;

namespace JiApp.Scheduler.Features.Services.UpdateService;

public sealed class UpdateServiceValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50)
            .Must(Validators.BeValidServiceCategory).WithMessage("Invalid service category");
        RuleFor(x => x.BaseDuration).GreaterThan(0);
        RuleFor(x => x.BasePrice.Amount).GreaterThanOrEqualTo(0);
    }
}