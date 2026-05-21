using FluentValidation;

namespace JiApp.Api.Features.Search.SearchVideos;

public sealed class SearchVideosValidator : AbstractValidator<SearchVideosRequest>
{
    public SearchVideosValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.MaxResults)
            .InclusiveBetween(1, 50)
            .When(x => x.MaxResults.HasValue);
    }
}
