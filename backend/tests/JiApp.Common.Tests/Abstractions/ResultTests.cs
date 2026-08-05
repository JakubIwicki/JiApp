using JiApp.Common.Abstractions;

namespace JiApp.Common.Tests.Abstractions;

public sealed class ResultTests
{
    [Fact]
    public void Succeeds_WithValue()
    {
        var result = Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void HasNoErrorCategory_WhenSuccess()
    {
        var result = Result<int>.Success(42);

        result.ErrorCategory.Should().BeNull();
    }

    [Fact]
    public void Fails_WithError_AndDefaultValue()
    {
        var result = Result<int>.Failure("boom");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("boom");
        result.Value.Should().Be(0);
    }

    [Fact]
    public void Fails_WithNullValue_OnReferenceType()
    {
        var result = Result<string>.Failure("boom");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void CarriesErrorCategory_WhenFailure_WithCategory()
    {
        var result = Result<string>.Failure("not found", ResultCategories.NotFound);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(ResultCategories.NotFound);
    }

    [Fact]
    public void LeavesErrorCategoryNull_WhenFailure_WithoutCategory()
    {
        var result = Result<int>.Failure("boom");

        result.ErrorCategory.Should().BeNull();
    }

    [Fact]
    public void HoldsNullValue_WhenSuccess_OnReferenceType()
    {
        var result = Result<string>.Success(null!);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void ExposesResultCategories_WithExpectedValues()
    {
        ResultCategories.NotFound.Should().Be("NotFound");
        ResultCategories.AccessDenied.Should().Be("AccessDenied");
        ResultCategories.Validation.Should().Be("Validation");
        ResultCategories.Conflict.Should().Be("Conflict");
        ResultCategories.BadGateway.Should().Be("BadGateway");
    }

    [Fact]
    public void ResultCategories_AreDistinct()
    {
        var categories = new[]
        {
            ResultCategories.NotFound,
            ResultCategories.AccessDenied,
            ResultCategories.Validation,
            ResultCategories.Conflict,
            ResultCategories.BadGateway
        };

        categories.Distinct().Should().HaveCount(categories.Length);
    }
}
