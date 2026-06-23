using JiApp.Scheduler.Features.Boards.UpdateBoard;

namespace JiApp.Scheduler.Tests.Features.Boards.UpdateBoard;

public sealed class UpdateBoardValidatorTests
{
    private sealed class Fixture
    {
        public UpdateBoardValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateBoardRequest("");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new UpdateBoardRequest(new string('a', 201));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidName_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new UpdateBoardRequest("Main Board");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
