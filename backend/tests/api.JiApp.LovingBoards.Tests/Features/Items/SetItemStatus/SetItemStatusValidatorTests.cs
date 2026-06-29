using FluentValidation;
using FluentValidation.TestHelper;
using api.JiApp.LovingBoards.Features.Items.SetItemStatus;

namespace api.JiApp.LovingBoards.Tests.Features.Items.SetItemStatus;

public sealed class SetItemStatusValidatorTests
{
    private readonly SetItemStatusValidator _validator = new();

    [Fact]
    public void EmptyStatus_Fails()
    {
        var request = new SetItemStatusRequest("");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void InvalidStatus_Fails()
    {
        var request = new SetItemStatusRequest("Bought");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Needed_Passes()
    {
        var request = new SetItemStatusRequest("Needed");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Completed_Passes()
    {
        var request = new SetItemStatusRequest("Completed");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Removed_Passes()
    {
        var request = new SetItemStatusRequest("Removed");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CaseInsensitive_Passes()
    {
        var request = new SetItemStatusRequest("completed");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
