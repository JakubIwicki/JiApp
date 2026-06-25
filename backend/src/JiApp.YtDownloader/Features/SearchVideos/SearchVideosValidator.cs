using FluentValidation;

namespace JiApp.YtDownloader.Features.SearchVideos;

public sealed class SearchVideosValidator : AbstractValidator<SearchVideosRequest>
{
    public SearchVideosValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Page.HasValue);
    }
}
