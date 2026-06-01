using JiApp.Scheduler.Features.Boards.AddBoardMember;

namespace JiApp.Scheduler.Tests.Features.Boards.AddBoardMember;

public sealed class AddBoardMemberValidatorTests
{
    private readonly AddBoardMemberValidator _sut = new();

    [Fact]
    public void Validate_WithZeroUserId_ReturnsError()
    {
        var request = new AddBoardMemberRequest(0);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNegativeUserId_ReturnsError()
    {
        var request = new AddBoardMemberRequest(-1);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidUserId_IsValid()
    {
        var request = new AddBoardMemberRequest(42);

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}