using JiApp.YtDownloader.Features.SearchVideos;

namespace JiApp.YtDownloader.Tests.Features.SearchVideos;

public sealed class SearchVideosValidatorTests
{
    private static SearchVideosValidator CreateValidator() => new();

    [Fact]
    public void Validator_RejectsQuery_LongerThan2048Characters()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest(new string('x', 2049), null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validator_AcceptsQuery_Of2048Characters()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest(new string('x', 2048), null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_AcceptsPastedYouTubeUrl_OfMaxLength()
    {
        var validator = CreateValidator();
        var pastedUrl = new string('x', 2000) + "youtube.com/watch?v=dQw4w9WgXcQ";
        var request = new SearchVideosRequest(pastedUrl, null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsEmptyQuery()
    {
        var validator = CreateValidator();

        var result = validator.Validate(new SearchVideosRequest(string.Empty, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validator_RejectsNegativePage()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest("test", -1);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Fact]
    public void Validator_AcceptsZeroPage()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest("test", 0);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
