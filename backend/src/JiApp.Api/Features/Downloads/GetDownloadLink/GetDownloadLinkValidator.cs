using FluentValidation;

namespace JiApp.Api.Features.Downloads.GetDownloadLink;

public sealed class GetDownloadLinkValidator : AbstractValidator<DownloadRequest>
{
    private static readonly string[] ValidHosts =
    [
        "www.youtube.com", "youtube.com", "m.youtube.com",
        "youtu.be", "youtube-nocookie.com", "www.youtube-nocookie.com"
    ];

    public GetDownloadLinkValidator()
    {
        RuleFor(x => x.VideoId)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.VideoUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(IsValidYouTubeUrl)
            .WithMessage("VideoUrl must be a valid YouTube URL (youtube.com/watch or youtu.be)");

        RuleFor(x => x.Title)
            .MaximumLength(300)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .When(x => x.Description is not null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(300)
            .When(x => x.ImageUrl is not null);
    }

    private static bool IsValidYouTubeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!Array.Exists(ValidHosts, h => uri.Host == h))
            return false;

        // youtu.be/<id> or youtube.com/watch?v=<id> or youtube.com/embed/<id>
        if (uri is { Host: "youtu.be", AbsolutePath.Length: > 1 })
            return true;

        var path = uri.AbsolutePath;
        if (path == "/watch" || path.StartsWith("/embed/"))
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return query["v"] is not null;
        }

        return false;
    }
}
