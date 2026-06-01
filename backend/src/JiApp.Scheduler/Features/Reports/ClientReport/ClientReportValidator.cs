using FluentValidation;

namespace JiApp.Scheduler.Features.Reports.ClientReport;

public sealed class ClientReportValidator : AbstractValidator<ClientReportRequest>
{
    private static readonly string[] AllowedSortBy = ["frequency", "totalSpent", "lastVisit"];

    public ClientReportValidator()
    {
        RuleFor(x => x.BoardId).GreaterThan(0);
        RuleFor(x => x.SortBy)
            .NotEmpty()
            .MaximumLength(50)
            .Must(s => AllowedSortBy.Contains(s))
            .WithMessage("SortBy must be one of: frequency, totalSpent, lastVisit");
    }
}