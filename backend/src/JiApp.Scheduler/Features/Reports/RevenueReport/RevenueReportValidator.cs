using FluentValidation;

namespace JiApp.Scheduler.Features.Reports.RevenueReport;

public sealed class RevenueReportValidator : AbstractValidator<RevenueReportRequest>
{
    private static readonly string[] AllowedGroupBy = ["weekend", "service", "location", "client"];

    public RevenueReportValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.GroupBy)
            .NotEmpty()
            .MaximumLength(50)
            .Must(s => AllowedGroupBy.Contains(s))
            .WithMessage("GroupBy must be one of: weekend, service, location, client");
        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber <= 366)
            .WithMessage("Date range must not exceed 366 days")
            .When(x => x.From <= x.To);
    }
}