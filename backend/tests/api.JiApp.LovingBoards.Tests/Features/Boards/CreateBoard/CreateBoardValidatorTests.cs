using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.CreateBoard;

public sealed class CreateBoardValidatorTests
{
    private static readonly LovingBoardsSettings Settings = new()
    {
        MaxBoardNameLength = 200
    };

    private sealed class Fixture
    {
        public CreateBoardValidator Sut => new(Settings);

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateBoardRequest("");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongName_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new CreateBoardRequest(new string('a', 201));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidName_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new CreateBoardRequest("Main Board");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
