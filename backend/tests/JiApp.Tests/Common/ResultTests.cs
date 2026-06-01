using FluentAssertions;
using JiApp.Common.Abstractions;
using Xunit;

namespace JiApp.Tests.Common;

public class ResultTests
{
    [Fact]
    public void Success_WithInt_ReturnsIsSuccessTrueAndValue()
    {
        var result = JiApp.Common.Abstractions.Result<int>.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithInt_ReturnsIsSuccessFalseAndError()
    {
        var result = JiApp.Common.Abstractions.Result<int>.Failure("something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().Be(0);
        result.Error.Should().Be("something went wrong");
    }

    [Fact]
    public void Success_WithString_ReturnsIsSuccessTrueAndValue()
    {
        var result = JiApp.Common.Abstractions.Result<string>.Success("hello");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_WithString_ReturnsIsSuccessFalseAndErrorAndNullValue()
    {
        var result = JiApp.Common.Abstractions.Result<string>.Failure("fail message");

        result.IsSuccess.Should().BeFalse();
        result.Value.Should().BeNull();
        result.Error.Should().Be("fail message");
    }

    [Fact]
    public void Result_is_class_not_struct()
    {
        // The "default trap" — a struct's default has IsSuccess=false, making it
        // indistinguishable from a valid failure. A class record's default is null,
        // making misuse obvious. Guard against accidental regression to struct.
        typeof(Result<int>).IsValueType.Should().BeFalse();
    }

    [Fact]
    public void Failure_DefaultErrorCategoryIsNull()
    {
        var result = Result<int>.Failure("something went wrong");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("something went wrong");
        result.ErrorCategory.Should().BeNull();
    }

    [Fact]
    public void Failure_WithErrorCategory_SetsCategory()
    {
        var result = Result<int>.Failure("yt-dlp error", "YoutubeDl");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("yt-dlp error");
        result.ErrorCategory.Should().Be("YoutubeDl");
    }
}