using FluentValidation;

namespace JiApp.YtDownloader.Features.DownloadHistory;

internal sealed class DownloadHistoryValidator : AbstractValidator<DownloadHistoryRequest>
{
    public DownloadHistoryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .When(x => x.Limit.HasValue);
    }
}
