using FluentAssertions;

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
        result.Value.Should().Be(default(int));
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
}
