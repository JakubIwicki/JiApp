using JiApp.Common.Abstractions;

namespace JiApp.Testing.Common.Assertions;

public static class ResultAssertions
{
    public static void AssertSuccess<T>(Result<T> result) =>
        result.IsSuccess.Should().BeTrue();

    public static void AssertSuccessWithValue<T>(Result<T> result, T expected) =>
        result.Value.Should().Be(expected);

    public static void AssertFailure<T>(Result<T> result, string category)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
    }

    public static void AssertFailureWithMessage<T>(Result<T> result, string category, string message)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
        result.Error.Should().Be(message);
    }

    public static void AssertNotFound<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.NotFound);

    public static void AssertAccessDenied<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.AccessDenied);

    public static void AssertValidationFailure<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Validation);

    public static void AssertConflict<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Conflict);
}
