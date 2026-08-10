using FluentValidation.Results;
using JiApp.YtDownloader.Features.SearchHistory;

namespace JiApp.YtDownloader.Tests.Features.SearchHistory;

public sealed class SearchHistoryValidatorTests : ValidatorTestBase
{
    [Theory]
    [InlineData(1)]
    [InlineData(50)]
    public void Validator_AcceptsLimit_AtBounds(int limit)
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(limit);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validator_RejectsLimit_ZeroOrNegative(int limit)
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(limit);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchHistoryRequest.Limit));
    }

    [Fact]
    public void Validator_RejectsLimit_Above50()
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(51);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(SearchHistoryRequest.Limit));
    }

    [Fact]
    public void Validator_AcceptsLimit_WhenNull()
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(null);

        result.IsValid.Should().BeTrue();
    }

    private sealed class Fixture
    {
        public SearchHistoryValidator Sut => new();

        public static Fixture Init() => new();

        public ValidationResult Validate(int? limit) => Sut.Validate(new SearchHistoryRequest(limit));
    }
}
