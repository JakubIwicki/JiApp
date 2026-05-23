using FluentAssertions;
using JiApp.Api.Features.Downloads.GetDownloadLink;
using Xunit;

namespace JiApp.Tests.Features.Downloads;

public class GetDownloadLinkValidatorTests
{
    private readonly GetDownloadLinkValidator _validator = new();

    private const string ValidVideoId = "dQw4w9WgXcQ";
    private const string ValidVideoUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            ValidVideoUrl,
            "Some Title",
            "Some Description",
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidRequestWithNullOptionalFields_Passes()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            ValidVideoUrl,
            null,
            null,
            null);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyVideoId_Fails()
    {
        var request = new DownloadRequest(
            "",
            ValidVideoUrl,
            "Some Title",
            "Some Description",
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoId");
    }

    [Fact]
    public void Validate_EmptyVideoUrl_Fails()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            "",
            "Some Title",
            "Some Description",
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoUrl");
    }

    [Fact]
    public void Validate_VideoUrlNotStartingWithYoutubeWatch_Fails()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            "https://example.com/video",
            "Some Title",
            "Some Description",
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "VideoUrl");
    }

    [Fact]
    public void Validate_TitleGreaterThan300Chars_Fails()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            ValidVideoUrl,
            new string('a', 301),
            "Some Description",
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
    }

    [Fact]
    public void Validate_DescriptionGreaterThan1000Chars_Fails()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            ValidVideoUrl,
            "Some Title",
            new string('a', 1001),
            "https://example.com/img.jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Description");
    }

    [Fact]
    public void Validate_ImageUrlGreaterThan300Chars_Fails()
    {
        var request = new DownloadRequest(
            ValidVideoId,
            ValidVideoUrl,
            "Some Title",
            "Some Description",
            "https://example.com/" + new string('a', 280) + ".jpg");
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ImageUrl");
    }
}