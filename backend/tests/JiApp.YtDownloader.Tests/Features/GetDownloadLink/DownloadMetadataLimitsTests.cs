using JiApp.YtDownloader.Features.GetDownloadLink;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public sealed class DownloadMetadataLimitsTests
{
    [Fact]
    public void TruncatesDescription_ToExactlyOneThousandCharacters_WhenLongerThanTheLimit()
    {
        var longDescription = new string('a', 1200);

        var truncated = GetDownloadLinkEndpoint.Truncate(longDescription, DownloadMetadataLimits.MaxDescriptionLength);

        truncated.Should().HaveLength(1000);
    }

    [Fact]
    public void DoesNotSplitSurrogatePair_WhenTheCutLandsOnALowSurrogate()
    {
        // U+1F3B5 (musical note) is the surrogate pair D83C DFB5, so with maxLength 2
        // the cut lands on the low surrogate and must back off to before the pair.
        const string value = "a🎵b";
        const int maxLength = 2;

        var truncated = GetDownloadLinkEndpoint.Truncate(value, maxLength);

        truncated.Should().Be("a");
    }

    [Fact]
    public void LeavesValuesWithinTheLimit_Unchanged()
    {
        var title = new string('a', DownloadMetadataLimits.MaxTitleLength);

        var truncated = GetDownloadLinkEndpoint.Truncate(title, DownloadMetadataLimits.MaxTitleLength);

        truncated.Should().Be(title);
    }

    [Fact]
    public void ReturnsNull_WhenInputIsNull()
    {
        var truncated = GetDownloadLinkEndpoint.Truncate(null, DownloadMetadataLimits.MaxDescriptionLength);

        truncated.Should().BeNull();
    }

    [Fact]
    public void ReturnsEmpty_WhenInputIsEmpty()
    {
        var truncated = GetDownloadLinkEndpoint.Truncate(string.Empty, DownloadMetadataLimits.MaxDescriptionLength);

        truncated.Should().BeEmpty();
    }

    [Fact]
    public void TruncatesAllMetadataFields_ToTheirConfiguredLimits()
    {
        var request = CreateRequest(
            new string('a', 500),
            new string('a', 1200),
            new string('a', 600));

        var truncated = GetDownloadLinkEndpoint.TruncateMetadata(request);

        truncated.Title.Should().HaveLength(DownloadMetadataLimits.MaxTitleLength);
        truncated.Description.Should().HaveLength(DownloadMetadataLimits.MaxDescriptionLength);
        truncated.ImageUrl.Should().HaveLength(DownloadMetadataLimits.MaxImageUrlLength);
    }

    [Fact]
    public void Validator_AcceptsMetadata_AtTheConfiguredLimits()
    {
        var validator = CreateValidator();
        var atLimit = CreateRequest(
            new string('a', DownloadMetadataLimits.MaxTitleLength),
            new string('a', DownloadMetadataLimits.MaxDescriptionLength),
            new string('a', DownloadMetadataLimits.MaxImageUrlLength));

        var result = validator.Validate(atLimit);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsMetadata_OverTheConfiguredLimits()
    {
        var validator = CreateValidator();
        var overLimit = CreateRequest(
            new string('a', DownloadMetadataLimits.MaxTitleLength + 1),
            new string('a', DownloadMetadataLimits.MaxDescriptionLength + 1),
            new string('a', DownloadMetadataLimits.MaxImageUrlLength + 1));

        var result = validator.Validate(overLimit);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.Title));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.Description));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.ImageUrl));
    }

    private static DownloadRequest CreateRequest(string? title, string? description, string? imageUrl) =>
        new("dQw4w9WgXcQ", "https://youtube.com/watch?v=dQw4w9WgXcQ", title, description, imageUrl);

    private static GetDownloadLinkValidator CreateValidator() => new();
}
