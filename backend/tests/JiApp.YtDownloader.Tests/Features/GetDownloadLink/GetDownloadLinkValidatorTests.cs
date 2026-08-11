using FluentValidation.Results;
using JiApp.YtDownloader.Features.GetDownloadLink;

namespace JiApp.YtDownloader.Tests.Features.GetDownloadLink;

public sealed class GetDownloadLinkValidatorTests : ValidatorTestBase
{
    private const string ValidVideoId = "dQw4w9WgXcQ";

    public static TheoryData<string> AllowedHostUrls => new()
    {
        { "https://www.youtube.com/watch?v=dQw4w9WgXcQ" },
        { "https://youtube.com/watch?v=dQw4w9WgXcQ" },
        { "https://m.youtube.com/watch?v=dQw4w9WgXcQ" },
        { "https://youtu.be/dQw4w9WgXcQ" },
        { "https://youtube-nocookie.com/watch?v=dQw4w9WgXcQ" },
        { "https://www.youtube-nocookie.com/watch?v=dQw4w9WgXcQ" },
    };

    public static TheoryData<string> LookAlikeHostUrls => new()
    {
        { "https://youtube.com.evil.tld/watch?v=dQw4w9WgXcQ" },
        { "https://evilyoutube.com/watch?v=dQw4w9WgXcQ" },
        { "https://sub.youtube.com/watch?v=dQw4w9WgXcQ" },
    };

    public static TheoryData<string> NonAbsoluteUrls => new()
    {
        { "youtube.com/watch?v=dQw4w9WgXcQ" },
        { "//youtube.com/watch?v=dQw4w9WgXcQ" },
        { "not a url" },
    };

    public static TheoryData<string> UnsupportedPathUrls => new()
    {
        { "https://www.youtube.com/shorts/dQw4w9WgXcQ" },
        { "https://youtube.com/@somechannel" },
        { "https://www.youtube.com/Watch?v=dQw4w9WgXcQ" },
        { "https://www.youtube.com//watch?v=dQw4w9WgXcQ" },
    };

    public static TheoryData<string> NonHttpSchemeUrls => new()
    {
        { "ssh://youtube.com/watch?v=dQw4w9WgXcQ" },
        { "ftp://youtu.be/dQw4w9WgXcQ" },
        { "file://youtube.com/watch?v=dQw4w9WgXcQ" },
    };

    public static TheoryData<string> ValidVideoIds => new()
    {
        { "dQw4w9WgXcQ" },
        { "abc-123_XYZ" },
    };

    public static TheoryData<string> InvalidVideoIds => new()
    {
        { "bad id!" },
        { "id.with.dots" },
        { "abc+def" },
    };

    public static TheoryData<string> BareYoutuBeUrls => new()
    {
        { "https://youtu.be/" },
        { "https://youtu.be" },
        { "https://youtu.be/?v=dQw4w9WgXcQ" },
    };

    [Theory]
    [MemberData(nameof(AllowedHostUrls))]
    public void Validator_AcceptsVideoUrl_OnEachAllowedHost(string url)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(LookAlikeHostUrls))]
    public void Validator_RejectsVideoUrl_OnLookAlikeHost(string url)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Fact]
    public void Validator_RejectsWatchUrl_WithoutVideoQueryParameter()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/watch");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Fact]
    public void Validator_RejectsWatchUrl_WhenOnlyOtherQueryParametersPresent()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/watch?foo=bar");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Fact]
    public void Validator_AcceptsWatchUrl_WhenVideoQueryParameterEmpty()
    {
        // Documents current behaviour: an empty `v` value ("?v=") parses to an empty string,
        // and `query["v"] is not null` treats it as present. Flagged as a likely defect.
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/watch?v=");

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(BareYoutuBeUrls))]
    public void Validator_RejectsBareYoutuBeUrl(string url)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Fact]
    public void Validator_AcceptsEmbedUrl_WhenVideoQueryParameterPresent()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/embed/dQw4w9WgXcQ?v=abc");

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsEmbedUrl_WithoutVideoQueryParameter()
    {
        // Documents current behaviour: the /embed/ branch requires a `v` query parameter,
        // so a real embed URL like /embed/<id> is rejected. Flagged as a likely defect.
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/embed/dQw4w9WgXcQ");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Fact]
    public void Validator_RejectsBareEmbedPath()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl("https://www.youtube.com/embed/");

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Theory]
    [MemberData(nameof(NonAbsoluteUrls))]
    public void Validator_RejectsNonAbsoluteVideoUrl(string url)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Theory]
    [MemberData(nameof(UnsupportedPathUrls))]
    public void Validator_RejectsVideoUrl_OnUnsupportedPath(string url)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoUrl));
    }

    [Theory]
    [MemberData(nameof(NonHttpSchemeUrls))]
    public void Validator_AcceptsVideoUrl_WithNonHttpScheme(string url)
    {
        // Documents current behaviour: the scheme is never checked, so any absolute URI
        // with an allowed host and a valid path passes. Flagged as a likely defect.
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoUrl(url);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(ValidVideoIds))]
    public void Validator_AcceptsVideoId_WithOnlyAlphanumericsDashesUnderscores(string videoId)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoId(videoId);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsVideoId_WhenEmpty()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoId(string.Empty);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoId));
    }

    [Theory]
    [MemberData(nameof(InvalidVideoIds))]
    public void Validator_RejectsVideoId_WithInvalidCharacters(string videoId)
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoId(videoId);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoId));
    }

    [Fact]
    public void Validator_AcceptsVideoId_AtMaxLength200()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoId(new string('a', 200));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsVideoId_WhenLongerThan200Characters()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateVideoId(new string('a', 201));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.VideoId));
    }

    [Fact]
    public void Validator_AcceptsTitle_AtMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(new string('a', DownloadMetadataLimits.MaxTitleLength), null, null);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsTitle_WhenOverMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(new string('a', DownloadMetadataLimits.MaxTitleLength + 1), null, null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.Title));
    }

    [Fact]
    public void Validator_AcceptsDescription_AtMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(null, new string('a', DownloadMetadataLimits.MaxDescriptionLength), null);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsDescription_WhenOverMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(null, new string('a', DownloadMetadataLimits.MaxDescriptionLength + 1), null);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.Description));
    }

    [Fact]
    public void Validator_AcceptsImageUrl_AtMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(null, null, new string('a', DownloadMetadataLimits.MaxImageUrlLength));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsImageUrl_WhenOverMaxLength()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(null, null, new string('a', DownloadMetadataLimits.MaxImageUrlLength + 1));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(DownloadRequest.ImageUrl));
    }

    [Fact]
    public void Validator_AcceptsMetadata_WhenAllFieldsNull()
    {
        var fixture = Fixture.Init();

        var result = fixture.ValidateMetadata(null, null, null);

        result.IsValid.Should().BeTrue();
    }

    private sealed class Fixture
    {
        public GetDownloadLinkValidator Sut => new();

        public static Fixture Init() => new();

        public ValidationResult ValidateVideoUrl(string videoUrl) =>
            Sut.Validate(new DownloadRequest(ValidVideoId, videoUrl, null, null, null));

        public ValidationResult ValidateVideoId(string videoId) =>
            Sut.Validate(new DownloadRequest(videoId, "https://youtube.com/watch?v=abc", null, null, null));

        public ValidationResult ValidateMetadata(string? title, string? description, string? imageUrl) =>
            Sut.Validate(new DownloadRequest(ValidVideoId, "https://youtube.com/watch?v=abc", title, description, imageUrl));
    }
}
