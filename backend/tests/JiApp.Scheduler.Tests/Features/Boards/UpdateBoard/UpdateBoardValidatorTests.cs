using JiApp.Scheduler.Features.Boards.UpdateBoard;

namespace JiApp.Scheduler.Tests.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidatorTests
{
    private readonly UpdateBoardValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var request = new UpdateBoardRequest("");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var request = new UpdateBoardRequest(new string('a', 201));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidName_IsValid()
    {
        var request = new UpdateBoardRequest("Main Board");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}