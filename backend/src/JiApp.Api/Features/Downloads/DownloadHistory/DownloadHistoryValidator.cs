using FluentValidation;

namespace JiApp.Api.Features.Downloads.DownloadHistory;

public sealed class DownloadHistoryValidator : AbstractValidator<DownloadHistoryRequest>
{
    public DownloadHistoryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .When(x => x.Limit.HasValue);
    }
}
