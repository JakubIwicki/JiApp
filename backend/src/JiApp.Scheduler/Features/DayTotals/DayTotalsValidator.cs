using FluentValidation;

namespace JiApp.Scheduler.Features.DayTotals;

public sealed class DayTotalsValidator : AbstractValidator<DayTotalsRequest>
{
    public DayTotalsValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.Date)
            .GreaterThanOrEqualTo(new DateOnly(2020, 1, 1))
            .WithMessage("Date must not be earlier than 2020-01-01");
    }
}
