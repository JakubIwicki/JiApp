using FluentValidation;
using FluentValidation.TestHelper;
using api.JiApp.LovingBoards.Common;
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
        var request = new UpdateItemRequest(Title: new Optional<string>(""));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title.Value);
    }

    [Fact]
    public void Title_TooLong_Fails()
    {
        var request = new UpdateItemRequest(Title: new Optional<string>(new string('x', Settings.MaxItemTitleLength + 1)));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Title.Value);
    }

    [Fact]
    public void Quantity_TooLong_Fails()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Valid"),
            Quantity: new Optional<string?>(new string('x', Settings.MaxQuantityLength + 1)));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Quantity.Value);
    }

    [Fact]
    public void Category_TooLong_Fails()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Valid"),
            Category: new Optional<string?>(new string('x', Settings.MaxCategoryLength + 1)));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Category.Value);
    }

    [Fact]
    public void Note_TooLong_Fails()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Valid"),
            Note: new Optional<string?>(new string('x', Settings.MaxNoteLength + 1)));
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Note.Value);
    }

    [Fact]
    public void ValidRequest_Passes()
    {
        var request = new UpdateItemRequest(Title: new Optional<string>("Milk"));
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Title_NotSet_Passes()
    {
        var request = new UpdateItemRequest();
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Title_NotSet_NoTitleErrors()
    {
        var request = new UpdateItemRequest();
        var result = _validator.TestValidate(request);
        // When Title is not set, the When(x => x.Title.IsSet) guard prevents validation
        result.ShouldNotHaveValidationErrorFor(x => x.Title.Value);
    }

    [Fact]
    public void Quantity_NotSet_Passes()
    {
        var request = new UpdateItemRequest(Title: new Optional<string>("Milk"));
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity.Value);
    }

    [Fact]
    public void Quantity_SetToNull_Passes()
    {
        var request = new UpdateItemRequest(
            Title: new Optional<string>("Milk"),
            Quantity: new Optional<string?>(null));
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.Quantity.Value);
    }
}
