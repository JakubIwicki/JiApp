using FluentAssertions;
using JiApp.Api.Features.Search.SearchVideos;

namespace JiApp.Tests.Features.Search;

public class SearchVideosValidatorTests
{
    private readonly SearchVideosValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var request = new SearchVideosRequest("test query", 10);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaxResultsOmitted_Passes()
    {
        var request = new SearchVideosRequest("test query", null);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyQuery_Fails()
    {
        var request = new SearchVideosRequest("", 10);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validate_QueryGreaterThan200Chars_Fails()
    {
        var request = new SearchVideosRequest(new string('a', 201), 10);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Query");
    }

    [Fact]
    public void Validate_MaxResults0_Fails()
    {
        var request = new SearchVideosRequest("test query", 0);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxResults");
    }

    [Fact]
    public void Validate_MaxResults51_Fails()
    {
        var request = new SearchVideosRequest("test query", 51);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "MaxResults");
    }

    [Fact]
    public void Validate_MaxResults1_Passes()
    {
        var request = new SearchVideosRequest("test query", 1);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MaxResults50_Passes()
    {
        var request = new SearchVideosRequest("test query", 50);
        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
