using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Boards.AddBoardMember;
using api.JiApp.LovingBoards.Features.Boards.CreateBoard;
using api.JiApp.LovingBoards.Features.Boards.DeleteBoard;
using api.JiApp.LovingBoards.Features.Boards.GetBoard;
using api.JiApp.LovingBoards.Features.Boards.ListBoards;
using api.JiApp.LovingBoards.Features.Boards.RemoveBoardMember;
using api.JiApp.LovingBoards.Features.Boards.UpdateBoard;
using api.JiApp.LovingBoards.Tests.Bases;

namespace api.JiApp.LovingBoards.Tests.Features.Boards;

public sealed class BoardHandlerTests : LovingBoardsHandlerTestBase
{
    private static readonly LovingBoardsSettings DefaultSettings = new()
    {
        ConnectionString = "Data Source=:memory:",
        Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" },
        MaxBoardsPerUser = 3,
        DefaultPageSize = 50,
        MaxBoardNameLength = 200
    };

    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly LovingBoardsSettings _settings;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _settings = DefaultSettings;
        }

        public CreateBoardHandler Sut => new(_dbContext, _settings, _currentUser);
        public GetBoardHandler GetBoard => new(_dbContext, _currentUser);
        public UpdateBoardHandler UpdateBoard => new(_dbContext, _currentUser);
        public DeleteBoardHandler DeleteBoard => new(_dbContext, _currentUser);
        public ListBoardsHandler ListBoards => new(_dbContext, _settings, _currentUser);
        public AddBoardMemberHandler AddBoardMember => new(_dbContext, _currentUser);
        public RemoveBoardMemberHandler RemoveBoardMember => new(_dbContext, _currentUser);

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(string name = "Test", long ownerUserId = 1L, List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, OwnerUserId = ownerUserId, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            return this;
        }

        public Fixture WithBoard(out long boardId, string name = "Test", long ownerUserId = 1L, List<long>? memberUserIds = null)
        {
            var board = new Board { Name = name, OwnerUserId = ownerUserId, MemberUserIds = memberUserIds ?? [1L] };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }
    }

    [Fact]
    public async Task CreateBoard_WithValidName_ReturnsBoardId()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
        var request = new CreateBoardRequest("My Board");

        var result = await sut.HandleAsync(request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateBoard_SetsCreatorAsOwnerAndMember()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;
        var request = new CreateBoardRequest("My Board");

        await sut.HandleAsync(request, CancellationToken.None);

        var board = await Db.Query<Board>().FirstAsync();
        board.OwnerUserId.Should().Be(1L);
        board.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task CreateBoard_EnforcesMaxBoardsPerUser()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.Sut;

        for (var i = 0; i < DefaultSettings.MaxBoardsPerUser; i++)
            await sut.HandleAsync(new CreateBoardRequest($"Board {i}"), CancellationToken.None);

        var result = await sut.HandleAsync(new CreateBoardRequest("Over Limit"), CancellationToken.None);

        AssertValidationFailure(result);
        result.Error.Should().Contain("Maximum number of boards");
    }

    [Fact]
    public async Task GetBoard_WithValidId_ReturnsBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Name.Should().Be("Test");
        result.Value.MemberUserIds.Should().Contain(1L);
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task GetBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task GetBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task UpdateBoard_WithValidId_UpdatesName()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Updated"), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.Name.Should().Be("Updated");
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(999L, new UpdateBoardRequest("Test"), CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task UpdateBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.UpdateBoard;

        var result = await sut.HandleAsync(boardId, new UpdateBoardRequest("Test"), CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task DeleteBoard_WithValidId_DeletesBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(boardId);
        var deleted = Db.Find<Board>(boardId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task DeleteBoard_WithInvalidId_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task DeleteBoard_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.DeleteBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task ListBoards_ReturnsOnlyUsersBoards()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithBoard(out var myBoardId, name: "Mine")
            .WithBoard(out var sharedBoardId, name: "Shared", memberUserIds: [1L, 2L]);
        var otherBoard = new Board { Name = "Theirs", OwnerUserId = 2L, MemberUserIds = [2L] };
        StoreInDb(otherBoard);
        var sut = fixture.ListBoards;

        var result = await sut.HandleAsync(CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Boards.Should().HaveCount(2);
        result.Value!.Boards.Select(b => b.Id).Should().Contain([myBoardId, sharedBoardId]);
        result.Value!.Boards.Select(b => b.Id).Should().NotContain(otherBoard.Id);
    }

    [Fact]
    public async Task ListBoards_SeedsDefaultBoards_WhenUserHasNone()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListBoards;

        var result = await sut.HandleAsync(CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Boards.Should().HaveCount(2);
        result.Value!.Boards.Select(b => b.Name).Should().Contain(["Groceries", "Home"]);
        result.Value!.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task ListBoards_DoesNotReSeed_OnSubsequentCalls()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.ListBoards;

        var first = await sut.HandleAsync(CancellationToken.None);
        var second = await sut.HandleAsync(CancellationToken.None);

        AssertSuccess(first);
        AssertSuccess(second);
        first.Value!.Boards.Should().HaveCount(2);
        second.Value!.Boards.Should().HaveCount(2);
    }

    [Fact]
    public async Task AddBoardMember_WithValidData_AddsMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(2L), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.MemberUserIds.Should().Contain([1L, 2L]);
    }

    [Fact]
    public async Task AddBoardMember_WithExistingMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.AddBoardMember;

        var result = await sut.HandleAsync(boardId, new AddBoardMemberRequest(1L), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already a member");
    }

    [Fact]
    public async Task RemoveBoardMember_RemovesMember()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 2L, CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<Board>(boardId);
        updated!.MemberUserIds.Should().ContainSingle().Which.Should().Be(1L);
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Board not found");
    }

    [Fact]
    public async Task RemoveBoardMember_WithInvalidBoard_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(999L, 2L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L, 3L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Access denied");
    }

    [Fact]
    public async Task RemoveBoardMember_ByNonMember_ReturnsAccessDeniedErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L, 3L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 3L, CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Member not found");
    }

    [Fact]
    public async Task RemoveBoardMember_WithNonExistentMember_ReturnsNotFoundErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [1L, 2L]);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        AssertNotFound(result);
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Cannot remove the last member");
    }

    [Fact]
    public async Task RemoveBoardMember_WhenLastMember_ReturnsConflictErrorCategory()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.RemoveBoardMember;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        AssertConflict(result);
    }

    // GetBoard with items

    [Fact]
    public async Task GetBoard_IncludesNonRemovedItems()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item 1", Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item 2", Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item 3", Status = BoardItemStatus.Removed, AddedByUserId = 1L });
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Items.Select(i => i.Title).Should().Contain(["Item 1", "Item 2"]);
        result.Value.Items.Select(i => i.Title).Should().NotContain("Item 3");
    }

    [Fact]
    public async Task GetBoard_ItemsIncludeAttributionAndStatus()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem
        {
            BoardId = boardId,
            Title = "Milk",
            Status = BoardItemStatus.Completed,
            AddedByUserId = 1L,
            CompletedByUserId = 2L,
            Category = "Dairy",
            Quantity = "2L"
        });
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        var item = result.Value!.Items.Should().ContainSingle().Subject;
        item.Title.Should().Be("Milk");
        item.Status.Should().Be("Completed");
        item.AddedByUserId.Should().Be(1L);
        item.CompletedByUserId.Should().Be(2L);
        item.Category.Should().Be("Dairy");
        item.Quantity.Should().Be("2L");
    }

    [Fact]
    public async Task GetBoard_ItemsOrderedByCategoryThenCreatedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        StoreInDb(new BoardItem { BoardId = boardId, Title = "B", Category = "Z", Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "A", Category = "A", Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "C", Category = "A", Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Items.Select(i => i.Title).Should().Equal(["A", "C", "B"]);
    }

    [Fact]
    public async Task GetBoard_EmptyItems_ReturnsEmptyList()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value!.Items.Should().BeEmpty();
    }

    // Weekly lazy reset

    [Fact]
    public async Task GetBoard_LazyReset_FlipsRecurringCompletedAndRemovedToNeeded()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var board = Db.Find<Board>(boardId);
        board!.LastWeeklyResetAt = DateTime.UtcNow.AddDays(-14);
        await DbContext.SaveChangesAsync();
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Completed", IsRecurring = true, Status = BoardItemStatus.Completed, CompletedByUserId = 2L, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Removed", IsRecurring = true, Status = BoardItemStatus.Removed, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "NonRecurring Completed", IsRecurring = false, Status = BoardItemStatus.Completed, CompletedByUserId = 2L, AddedByUserId = 1L });
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Recurring Needed", IsRecurring = true, Status = BoardItemStatus.Needed, AddedByUserId = 1L });
        var sut = fixture.GetBoard;
        var before = DateTime.UtcNow;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        // Recurring Completed → Needed (visible)
        var rc = result.Value!.Items.Single(i => i.Title == "Recurring Completed");
        rc.Status.Should().Be("Needed");
        rc.CompletedByUserId.Should().BeNull();
        // Recurring Removed → Needed (visible now)
        var rr = result.Value.Items.Single(i => i.Title == "Recurring Removed");
        rr.Status.Should().Be("Needed");
        rr.RemovedAt.Should().BeNull();
        // Non-recurring Completed stays Completed
        var nc = result.Value.Items.Single(i => i.Title == "NonRecurring Completed");
        nc.Status.Should().Be("Completed");
        nc.CompletedByUserId.Should().Be(2L);
        // Recurring Needed stays Needed
        var rn = result.Value.Items.Single(i => i.Title == "Recurring Needed");
        rn.Status.Should().Be("Needed");
        // Board.LastWeeklyResetAt bumped
        var updatedBoard = Db.Find<Board>(boardId);
        updatedBoard!.LastWeeklyResetAt.Should().NotBeNull();
        updatedBoard.LastWeeklyResetAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task GetBoard_LazyReset_IsIdempotentWithinSameWeek()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var board = Db.Find<Board>(boardId);
        board!.LastWeeklyResetAt = DateTime.UtcNow.AddDays(-14);
        await DbContext.SaveChangesAsync();
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item", IsRecurring = true, Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        var sut = fixture.GetBoard;

        var first = await sut.HandleAsync(boardId, CancellationToken.None);
        var firstResetAt = Db.Find<Board>(boardId)!.LastWeeklyResetAt;

        // Manually revert the item back to Completed to see if a second call resets again
        var item = await Db.Query<BoardItem>().FirstAsync(i => i.Title == "Item");
        item.Status = BoardItemStatus.Completed;
        item.CompletedByUserId = 2L;
        await DbContext.SaveChangesAsync();

        var second = await sut.HandleAsync(boardId, CancellationToken.None);
        var secondResetAt = Db.Find<Board>(boardId)!.LastWeeklyResetAt;

        AssertSuccess(first);
        AssertSuccess(second);
        // Second call should NOT reset — same ISO week
        var secondItem = second.Value!.Items.Single(i => i.Title == "Item");
        secondItem.Status.Should().Be("Completed"); // still Completed, not flipped
        secondResetAt.Should().Be(firstResetAt);    // LastWeeklyResetAt unchanged
    }

    [Fact]
    public async Task GetBoard_LazyReset_DoesNotFireWhenResetNotDue()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var board = Db.Find<Board>(boardId);
        board!.LastWeeklyResetAt = DateTime.UtcNow; // within current week
        await DbContext.SaveChangesAsync();
        StoreInDb(new BoardItem { BoardId = boardId, Title = "Item", IsRecurring = true, Status = BoardItemStatus.Completed, AddedByUserId = 1L });
        var sut = fixture.GetBoard;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        var item = result.Value!.Items.Single(i => i.Title == "Item");
        item.Status.Should().Be("Completed"); // unchanged
    }
}
