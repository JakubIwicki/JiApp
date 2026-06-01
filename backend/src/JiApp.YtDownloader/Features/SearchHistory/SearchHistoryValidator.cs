using FluentValidation;

namespace JiApp.YtDownloader.Features.SearchHistory;

public sealed class SearchHistoryValidator : AbstractValidator<SearchHistoryRequest>
{
    public SearchHistoryValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(1, 50)
            .When(x => x.Limit.HasValue);
    }
}
