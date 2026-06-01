using FluentValidation.Results;

namespace JiApp.Common.Abstractions;

public static class ValidationExtensions
{
    public static string[] ErrorMessages(this ValidationResult result)
    {
        var errors = result.Errors;
        var messages = new string[errors.Count];
        for (var i = 0; i < errors.Count; i++)
        {
            messages[i] = errors[i].ErrorMessage;
        }

        return messages;
    }
}