using JiApp.Scheduler.Features.Boards.CreateBoard;

namespace JiApp.Scheduler.Tests.Features.Boards.CreateBoard;

public sealed class CreateBoardValidatorTests
{
    private readonly CreateBoardValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new CreateBoardRequest("");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var request = new CreateBoardRequest(new string('a', 201));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidName_IsValid()
    {
        var request = new CreateBoardRequest("Main Board");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}