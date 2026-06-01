using JiApp.YtDownloader.Features.SearchVideos;

namespace JiApp.YtDownloader.Tests.Features.SearchVideos;

public class SearchVideosValidatorTests
{
    private static SearchVideosValidator CreateValidator() => new();

    [Fact]
    public void Validator_rejects_query_longer_than_100_characters()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest(new string('x', 101), null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validator_accepts_query_of_100_characters()
    {
        var validator = CreateValidator();
        var request = new SearchVideosRequest(new string('x', 100), null);

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_rejects_empty_query()
    {
        var validator = CreateValidator();

        var result = validator.Validate(new SearchVideosRequest(string.Empty, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }
}
