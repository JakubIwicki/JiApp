using FluentValidation;

namespace JiApp.YtDownloader.Features.SearchVideos;

public sealed class SearchVideosValidator : AbstractValidator<SearchVideosRequest>
{
    public SearchVideosValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50)
            .When(x => x.MaxResults.HasValue);
    }
}
