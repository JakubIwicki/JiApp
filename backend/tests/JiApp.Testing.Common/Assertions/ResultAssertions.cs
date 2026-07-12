using JiApp.Common.Abstractions;

namespace JiApp.Testing.Common.Assertions;

public static class ResultAssertions
{
    public static T AssertSuccess<T>(Result<T> result)
    {
        result.IsSuccess.Should().BeTrue();
        return result.Value!;
    }

    public static T AssertSuccessWithValue<T>(Result<T> result, T expected)
    {
        result.Value.Should().Be(expected);
        return result.Value!;
    }

    public static string AssertFailure<T>(Result<T> result, string category)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
        return result.Error!;
    }

    public static string AssertFailureWithMessage<T>(Result<T> result, string category, string message)
    {
        result.IsSuccess.Should().BeFalse();
        result.ErrorCategory.Should().Be(category);
        result.Error.Should().Be(message);
        return result.Error!;
    }

    public static string AssertNotFound<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.NotFound);

    public static string AssertAccessDenied<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.AccessDenied);

    public static string AssertValidationFailure<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Validation);

    public static string AssertConflict<T>(Result<T> result) =>
        AssertFailure(result, ResultCategories.Conflict);
}
