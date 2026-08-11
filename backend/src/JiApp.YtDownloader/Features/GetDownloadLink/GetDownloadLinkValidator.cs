using FluentValidation;

namespace JiApp.YtDownloader.Features.GetDownloadLink;

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
            .MaximumLength(200)
            .Matches("^[a-zA-Z0-9_-]+$")
            .WithMessage("VideoId must contain only letters, digits, hyphens, and underscores.");

        RuleFor(x => x.VideoUrl)
            .NotEmpty()
            .MaximumLength(2048)
            .Must(IsValidYouTubeUrl)
            .WithMessage("VideoUrl must be a valid YouTube URL (youtube.com/watch or youtu.be)");

        RuleFor(x => x.Title)
            .MaximumLength(DownloadMetadataLimits.MaxTitleLength)
            .When(x => x.Title is not null);

        RuleFor(x => x.Description)
            .MaximumLength(DownloadMetadataLimits.MaxDescriptionLength)
            .When(x => x.Description is not null);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(DownloadMetadataLimits.MaxImageUrlLength)
            .When(x => x.ImageUrl is not null);
    }

    private static bool IsValidYouTubeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            return false;

        if (!Array.Exists(ValidHosts, h => uri.Host == h))
            return false;

        if (uri is { Host: "youtu.be", AbsolutePath.Length: > 1 })
            return true;

        var path = uri.AbsolutePath;
        if (path == "/watch")
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            return !string.IsNullOrEmpty(query["v"]);
        }

        if (path.StartsWith("/embed/", StringComparison.Ordinal) && path.Length > "/embed/".Length)
            return true;

        return false;
    }
}
