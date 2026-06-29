using FluentValidation;
using FluentValidation.TestHelper;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Items.CreateItem;

namespace api.JiApp.LovingBoards.Tests.Features.Items.CreateItem;

public sealed class CreateItemValidatorTests
{
    private static readonly LovingBoardsSettings Settings = new()
    {
        MaxItemTitleLength = 200,
        MaxQuantityLength = 50,
        MaxCategoryLength = 100,
        MaxNoteLength = 1000
    };

    private readonly CreateItemValidator _validator = new(Settings);

    [Fact]
    public void Title_Empty_Fails()
    {
        var request = new CreateItemRequest("");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_TooLong_Fails()
    {
        var request = new CreateItemRequest(new string('x', Settings.MaxItemTitleLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Quantity_TooLong_Fails()
    {
        var request = new CreateItemRequest("Valid", Quantity: new string('x', Settings.MaxQuantityLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Category_TooLong_Fails()
    {
        var request = new CreateItemRequest("Valid", Category: new string('x', Settings.MaxCategoryLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Note_TooLong_Fails()
    {
        var request = new CreateItemRequest("Valid", Note: new string('x', Settings.MaxNoteLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new CreateItemRequest("Milk", "2L", "Dairy", "Low fat");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void NullOptionals_Passes()
    {
        var request = new CreateItemRequest("Milk");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
