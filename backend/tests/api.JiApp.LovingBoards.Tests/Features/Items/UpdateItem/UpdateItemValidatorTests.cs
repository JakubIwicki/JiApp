using FluentValidation;
using FluentValidation.TestHelper;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Items.UpdateItem;

namespace api.JiApp.LovingBoards.Tests.Features.Items.UpdateItem;

public sealed class UpdateItemValidatorTests
{
    private static readonly LovingBoardsSettings Settings = new()
    {
        MaxItemTitleLength = 200,
        MaxQuantityLength = 50,
        MaxCategoryLength = 100,
        MaxNoteLength = 1000
    };

    private readonly UpdateItemValidator _validator = new(Settings);

    [Fact]
    public void Title_Empty_Fails()
    {
        var request = new UpdateItemRequest("");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Title_TooLong_Fails()
    {
        var request = new UpdateItemRequest(new string('x', Settings.MaxItemTitleLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void Quantity_TooLong_Fails()
    {
        var request = new UpdateItemRequest("Valid", Quantity: new string('x', Settings.MaxQuantityLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Category_TooLong_Fails()
    {
        var request = new UpdateItemRequest("Valid", Category: new string('x', Settings.MaxCategoryLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public void Note_TooLong_Fails()
    {
        var request = new UpdateItemRequest("Valid", Note: new string('x', Settings.MaxNoteLength + 1));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new UpdateItemRequest("Milk");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
