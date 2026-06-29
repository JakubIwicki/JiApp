using api.JiApp.LovingBoards.Features.Boards.AddBoardMember;

namespace api.JiApp.LovingBoards.Tests.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberValidatorTests
{
    private sealed class Fixture
    {
        public AddBoardMemberValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithZeroUserId_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new AddBoardMemberRequest(0);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNegativeUserId_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new AddBoardMemberRequest(-1);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidUserId_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new AddBoardMemberRequest(42);

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
