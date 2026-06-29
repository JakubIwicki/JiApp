namespace api.JiApp.LovingBoards.Tests.Domain;

public sealed class BoardItemTests
{
    [Fact]
    public void BoardItem_WithValidData_HasCorrectDefaults()
    {
        var item = new BoardItem
        {
            BoardId = 1L,
            Title = "Milk",
            AddedByUserId = 1L
        };

        item.BoardId.Should().Be(1L);
        item.Title.Should().Be("Milk");
        item.AddedByUserId.Should().Be(1L);
        item.Status.Should().Be(BoardItemStatus.Needed);
        item.IsRecurring.Should().BeFalse();
        item.Quantity.Should().BeNull();
        item.Category.Should().BeNull();
        item.Note.Should().BeNull();
        item.AssigneeUserId.Should().BeNull();
        item.ExpiryDate.Should().BeNull();
        item.CompletedByUserId.Should().BeNull();
        item.RemovedAt.Should().BeNull();
        item.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        item.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void BoardItemStatus_EnumValues_AreCorrect()
    {
        Enum.GetValues<BoardItemStatus>().Should().BeEquivalentTo([
            BoardItemStatus.Needed,
            BoardItemStatus.Completed,
            BoardItemStatus.Removed
        ]);
    }
}
