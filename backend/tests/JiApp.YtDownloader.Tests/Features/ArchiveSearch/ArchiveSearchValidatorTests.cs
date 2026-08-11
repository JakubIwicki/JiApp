using FluentValidation.Results;
using JiApp.YtDownloader.Features.ArchiveSearch;

namespace JiApp.YtDownloader.Tests.Features.ArchiveSearch;

public sealed class ArchiveSearchValidatorTests : ValidatorTestBase
{
    [Fact]
    public void Validator_AcceptsId_WhenGreaterThanZero()
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(1);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_RejectsId_WhenZero()
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(0);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ArchiveSearchRequest.Id));
    }

    [Fact]
    public void Validator_RejectsId_WhenNegative()
    {
        var fixture = Fixture.Init();

        var result = fixture.Validate(-1);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ArchiveSearchRequest.Id));
    }

    private sealed class Fixture
    {
        public ArchiveSearchValidator Sut => new();

        public static Fixture Init() => new();

        public ValidationResult Validate(long id) => Sut.Validate(new ArchiveSearchRequest(id));
    }
}
