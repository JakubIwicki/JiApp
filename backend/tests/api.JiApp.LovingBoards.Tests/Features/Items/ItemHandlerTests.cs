using JiApp.Common.Abstractions;
using JiApp.Common.Services;
using api.JiApp.LovingBoards.Common;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Items.ClearCompleted;
using api.JiApp.LovingBoards.Features.Items.CreateItem;
using api.JiApp.LovingBoards.Features.Items.DeleteItem;
using api.JiApp.LovingBoards.Features.Items.SetItemStatus;
using api.JiApp.LovingBoards.Features.Items.UpdateItem;
using api.JiApp.LovingBoards.Realtime;
using api.JiApp.LovingBoards.Tests.Bases;
using api.JiApp.LovingBoards.Tests.Realtime;

namespace api.JiApp.LovingBoards.Tests.Features.Items;

public sealed class ItemHandlerTests : LovingBoardsHandlerTestBase
{
    private static readonly LovingBoardsSettings DefaultSettings = new()
    {
        ConnectionString = "Data Source=:memory:",
        Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" },
        MaxBoardsPerUser = 3,
        DefaultPageSize = 50,
        MaxBoardNameLength = 200,
        MaxItemsPerBoard = 3,
        MaxItemTitleLength = 200,
        MaxQuantityLength = 50,
        MaxCategoryLength = 100,
        MaxNoteLength = 1000
    };

    private sealed class Fixture
    {
        private readonly ILovingBoardsDbContext _dbContext;
        private readonly TestDb _testDb;
        private readonly ICurrentUserService _currentUser;
        private readonly LovingBoardsSettings _settings;
        private readonly IBoardBroadcaster _broadcaster;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _settings = DefaultSettings;
            _broadcaster = new NoOpBoardBroadcaster();
        }

        public CreateItemHandler CreateItem => new(_dbContext, _settings, _currentUser, _broadcaster);
        public UpdateItemHandler UpdateItem => new(_dbContext, _currentUser, _broadcaster);
        public SetItemStatusHandler SetItemStatus => new(_dbContext, _currentUser, _broadcaster);
        public DeleteItemHandler DeleteItem => new(_dbContext, _currentUser, _broadcaster);
        public ClearCompletedHandler ClearCompleted => new(_dbContext, _currentUser, _broadcaster);

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

        public Fixture WithItem(long boardId, string title = "Test Item", BoardItemStatus status = BoardItemStatus.Needed, long addedByUserId = 1L)
        {
            var item = new BoardItem { BoardId = boardId, Title = title, Status = status, AddedByUserId = addedByUserId };
            _testDb.Store(item);
            return this;
        }

        public Fixture WithItem(out long itemId, long boardId, string title = "Test Item", BoardItemStatus status = BoardItemStatus.Needed, long addedByUserId = 1L)
        {
            var item = new BoardItem { BoardId = boardId, Title = title, Status = status, AddedByUserId = addedByUserId };
            _testDb.Store(item);
            itemId = item.Id;
            return this;
        }
    }

    // CreateItem

    [Fact]
    public async Task CreateItem_WithValidData_ReturnsItemId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.CreateItem;
        var request = new CreateItemRequest("Milk");

        var result = await sut.HandleAsync(boardId, request, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateItem_SetsAddedByAndNeededStatus()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.CreateItem;
        var request = new CreateItemRequest("Milk");

        await sut.HandleAsync(boardId, request, CancellationToken.None);

        var item = await Db.Query<BoardItem>().FirstAsync();
        item.AddedByUserId.Should().Be(1L);
        item.Status.Should().Be(BoardItemStatus.Needed);
        item.BoardId.Should().Be(boardId);
    }

    [Fact]
    public async Task CreateItem_WithAllOptionalFields_SetsThem()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.CreateItem;
        var expiry = DateTime.UtcNow.AddDays(7);
        var request = new CreateItemRequest("Milk", "2L", "Dairy", "Low fat", 2L, expiry, true);

        await sut.HandleAsync(boardId, request, CancellationToken.None);

        var item = await Db.Query<BoardItem>().FirstAsync();
        item.Quantity.Should().Be("2L");
        item.Category.Should().Be("Dairy");
        item.Note.Should().Be("Low fat");
        item.AssigneeUserId.Should().Be(2L);
        item.ExpiryDate.Should().Be(expiry);
        item.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task CreateItem_EnforcesMaxItemsPerBoard()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.CreateItem;

        for (var i = 0; i < DefaultSettings.MaxItemsPerBoard; i++)
            await sut.HandleAsync(boardId, new CreateItemRequest($"Item {i}"), CancellationToken.None);

        var result = await sut.HandleAsync(boardId, new CreateItemRequest("Over Limit"), CancellationToken.None);

        AssertValidationFailure(result);
        result.Error.Should().Contain("Maximum number of items");
        AssertEntityCount<BoardItem>((LovingBoardsDbContext)DbContext, DefaultSettings.MaxItemsPerBoard);
    }

    [Fact]
    public async Task CreateItem_DoesNotCountRemovedItemsTowardLimit()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.CreateItem;

        for (var i = 0; i < DefaultSettings.MaxItemsPerBoard; i++)
            await sut.HandleAsync(boardId, new CreateItemRequest($"Item {i}"), CancellationToken.None);

        // Soft-remove one item
        var first = await Db.Query<BoardItem>().FirstAsync();
        first.Status = BoardItemStatus.Removed;
        first.RemovedAt = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        var result = await sut.HandleAsync(boardId, new CreateItemRequest("New One"), CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CreateItem_NonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.CreateItem;

        var result = await sut.HandleAsync(boardId, new CreateItemRequest("Milk"), CancellationToken.None);

        AssertAccessDenied(result);
        AssertNoEntityInDb<BoardItem>((LovingBoardsDbContext)DbContext);
    }

    [Fact]
    public async Task CreateItem_BoardNotFound_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db);
        var sut = fixture.CreateItem;

        var result = await sut.HandleAsync(999L, new CreateItemRequest("Milk"), CancellationToken.None);

        AssertNotFound(result);
        AssertNoEntityInDb<BoardItem>((LovingBoardsDbContext)DbContext);
    }

    // UpdateItem

    [Fact]
    public async Task UpdateItem_UpdatesFieldsAndBumpsUpdatedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var sut = fixture.UpdateItem;
        var before = DateTime.UtcNow;

        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(
            Title: new Optional<string>("Updated"),
            Quantity: new Optional<string?>("3x"),
            Category: new Optional<string?>("NewCat"),
            Note: new Optional<string?>("Note")), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Title.Should().Be("Updated");
        updated.Quantity.Should().Be("3x");
        updated.Category.Should().Be("NewCat");
        updated.Note.Should().Be("Note");
        updated.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task UpdateItem_DoesNotChangeStatus()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Completed);
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(Title: new Optional<string>("Updated")), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Status.Should().Be(BoardItemStatus.Completed);
    }

    [Fact]
    public async Task UpdateItem_ItemNotFound_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId, 999L, new UpdateItemRequest(Title: new Optional<string>("Updated")), CancellationToken.None);

        AssertNotFound(result);
        AssertEntityDoesNotExist<BoardItem>((LovingBoardsDbContext)DbContext, 999L);
    }

    [Fact]
    public async Task UpdateItem_ItemOnDifferentBoard_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db)
            .WithBoard(out var boardId1)
            .WithBoard(out var boardId2);
        fixture.WithItem(out var itemId, boardId2);
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId1, itemId, new UpdateItemRequest(Title: new Optional<string>("Updated")), CancellationToken.None);

        AssertNotFound(result);
        var reloaded = Db.FindFresh<BoardItem>(itemId);
        reloaded!.Title.Should().Be("Test Item");
        reloaded.BoardId.Should().Be(boardId2);
    }

    [Fact]
    public async Task UpdateItem_NonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId, 1L, new UpdateItemRequest(Title: new Optional<string>("Updated")), CancellationToken.None);

        AssertAccessDenied(result);
    }

    [Fact]
    public async Task UpdateItem_PartialUpdate_PreservesUnsetFields()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        // Set all optional fields on the existing item
        var item = Db.Find<BoardItem>(itemId);
        item!.Quantity = "2L";
        item.Category = "Dairy";
        item.Note = "Low fat";
        item.AssigneeUserId = 2L;
        item.ExpiryDate = DateTime.UtcNow.AddDays(7);
        item.IsRecurring = true;
        await DbContext.SaveChangesAsync();
        ((LovingBoardsDbContext)DbContext).ChangeTracker.Clear();

        var sut = fixture.UpdateItem;

        // Patch only Title — all other fields should remain unchanged
        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(Title: new Optional<string>("New Title")), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Title.Should().Be("New Title");
        updated.Quantity.Should().Be("2L");
        updated.Category.Should().Be("Dairy");
        updated.Note.Should().Be("Low fat");
        updated.AssigneeUserId.Should().Be(2L);
        updated.ExpiryDate.Should().NotBeNull();
        updated.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_ExplicitNull_ClearsNullableField()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var item = Db.Find<BoardItem>(itemId);
        item!.Quantity = "2L";
        await DbContext.SaveChangesAsync();
        ((LovingBoardsDbContext)DbContext).ChangeTracker.Clear();

        var sut = fixture.UpdateItem;

        // Patch Quantity to null explicitly
        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(
            Title: new Optional<string>("Title"),
            Quantity: new Optional<string?>(null)), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Quantity.Should().BeNull();
    }

    [Fact]
    public async Task UpdateItem_PartialUpdate_BumpsUpdatedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var before = DateTime.UtcNow;
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(Title: new Optional<string>("X")), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.UpdatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task UpdateItem_PatchOneField_DoesNotClobberOthers()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var item = Db.Find<BoardItem>(itemId);
        item!.Quantity = "500g";
        item.Category = "Bakery";
        item.Note = "Fresh";
        item.AssigneeUserId = 3L;
        item.IsRecurring = true;
        await DbContext.SaveChangesAsync();
        ((LovingBoardsDbContext)DbContext).ChangeTracker.Clear();

        var sut = fixture.UpdateItem;

        // Patch only Quantity — Title, Category, Note, Assignee, IsRecurring must survive
        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(
            Title: new Optional<string>("Title"),
            Quantity: new Optional<string?>("1kg")), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Quantity.Should().Be("1kg");
        updated.Title.Should().Be("Title");
        updated.Category.Should().Be("Bakery");
        updated.Note.Should().Be("Fresh");
        updated.AssigneeUserId.Should().Be(3L);
        updated.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_AllFieldsSet_BehavesAsFullUpdate()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var sut = fixture.UpdateItem;
        var expiry = DateTime.UtcNow.AddDays(1);

        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(
            Title: new Optional<string>("Full"),
            Quantity: new Optional<string?>("3x"),
            Category: new Optional<string?>("Cat"),
            Note: new Optional<string?>("Note"),
            AssigneeUserId: new Optional<long?>(2L),
            ExpiryDate: new Optional<DateTime?>(expiry),
            IsRecurring: new Optional<bool>(true)), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Title.Should().Be("Full");
        updated.Quantity.Should().Be("3x");
        updated.Category.Should().Be("Cat");
        updated.Note.Should().Be("Note");
        updated.AssigneeUserId.Should().Be(2L);
        updated.ExpiryDate.Should().Be(expiry);
        updated.IsRecurring.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateItem_NoFieldsSet_OnlyBumpsUpdatedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, title: "Original");
        var before = DateTime.UtcNow;
        var sut = fixture.UpdateItem;

        var result = await sut.HandleAsync(boardId, itemId, new UpdateItemRequest(), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Title.Should().Be("Original");
        updated.UpdatedAt.Should().BeOnOrAfter(before);
    }

    // SetItemStatus

    [Fact]
    public async Task SetItemStatus_NeededToCompleted_SetsCompletedByUserId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Needed);
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Completed"), CancellationToken.None);

        AssertSuccess(result);
        var item = Db.Find<BoardItem>(itemId);
        item!.Status.Should().Be(BoardItemStatus.Completed);
        item.CompletedByUserId.Should().Be(1L);
        item.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetItemStatus_CompletedToNeeded_ClearsCompletedByUserId()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Completed);
        var item = await DbContext.BoardItems.FirstAsync(i => i.Id == itemId);
        item.CompletedByUserId = 2L;
        await DbContext.SaveChangesAsync();
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Needed"), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Status.Should().Be(BoardItemStatus.Needed);
        updated.CompletedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task SetItemStatus_ToRemoved_SetsRemovedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Needed);
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Removed"), CancellationToken.None);

        AssertSuccess(result);
        var item = Db.Find<BoardItem>(itemId);
        item!.Status.Should().Be(BoardItemStatus.Removed);
        item.RemovedAt.Should().NotBeNull();
        item.RemovedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SetItemStatus_Idempotent_ReturnsSuccessWithoutChange()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Completed);
        var sut = fixture.SetItemStatus;
        var before = Db.Find<BoardItem>(itemId)!.UpdatedAt;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Completed"), CancellationToken.None);

        AssertSuccess(result);
        var item = Db.Find<BoardItem>(itemId);
        item!.UpdatedAt.Should().Be(before);
    }

    [Fact]
    public async Task SetItemStatus_ItemNotFound_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, 999L, new SetItemStatusRequest("Completed"), CancellationToken.None);

        AssertNotFound(result);
        AssertEntityDoesNotExist<BoardItem>((LovingBoardsDbContext)DbContext, 999L);
    }

    [Fact]
    public async Task SetItemStatus_NonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, 1L, new SetItemStatusRequest("Completed"), CancellationToken.None);

        AssertAccessDenied(result);
        AssertNoEntityInDb<BoardItem>((LovingBoardsDbContext)DbContext);
    }

    // DeleteItem

    [Fact]
    public async Task DeleteItem_HardDeletesItem()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);
        var sut = fixture.DeleteItem;

        var result = await sut.HandleAsync(boardId, itemId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(itemId);
        var deleted = Db.Find<BoardItem>(itemId);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteItem_ItemNotFound_ReturnsNotFound()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        var sut = fixture.DeleteItem;

        var result = await sut.HandleAsync(boardId, 999L, CancellationToken.None);

        AssertNotFound(result);
        AssertEntityDoesNotExist<BoardItem>((LovingBoardsDbContext)DbContext, 999L);
    }

    [Fact]
    public async Task DeleteItem_NonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.DeleteItem;

        var result = await sut.HandleAsync(boardId, 1L, CancellationToken.None);

        AssertAccessDenied(result);
        AssertNoEntityInDb<BoardItem>((LovingBoardsDbContext)DbContext);
    }

    // ClearCompleted

    [Fact]
    public async Task ClearCompleted_SoftRemovesOnlyCompletedItems()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        fixture.WithItem(boardId, "Needed 1", BoardItemStatus.Needed);
        fixture.WithItem(boardId, "Completed 1", BoardItemStatus.Completed);
        fixture.WithItem(boardId, "Completed 2", BoardItemStatus.Completed);
        fixture.WithItem(boardId, "Removed 1", BoardItemStatus.Removed);
        var sut = fixture.ClearCompleted;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(2);

        var items = await Db.Query<BoardItem>().ToListAsync();
        items.Where(i => i.Status == BoardItemStatus.Removed).Should().HaveCount(3); // 2 cleared + 1 was already removed
        items.Single(i => i.Title == "Needed 1").Status.Should().Be(BoardItemStatus.Needed);
    }

    [Fact]
    public async Task ClearCompleted_ReturnsZeroWhenNoneCompleted()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        fixture.WithItem(boardId, "Needed 1", BoardItemStatus.Needed);
        var sut = fixture.ClearCompleted;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertSuccess(result);
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task ClearCompleted_NonMember_ReturnsAccessDenied()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId, memberUserIds: [2L]);
        var sut = fixture.ClearCompleted;

        var result = await sut.HandleAsync(boardId, CancellationToken.None);

        AssertAccessDenied(result);
        AssertNoEntityInDb<BoardItem>((LovingBoardsDbContext)DbContext);
    }

    // Publish events

    [Fact]
    public async Task CreateItem_PublishesItemAdded()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new CreateItemHandler(DbContext, DefaultSettings, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        StoreInDb(new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = [1L] });
        var boardId = Db.Query<Board>().First().Id;

        await handler.HandleAsync(boardId, new CreateItemRequest("Milk"), CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].BoardId.Should().Be(boardId);
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.ItemAdded);
    }

    [Fact]
    public async Task UpdateItem_PublishesItemUpdated()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new UpdateItemHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);

        await handler.HandleAsync(boardId, itemId, new UpdateItemRequest(Title: new Optional<string>("Updated")), CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.ItemUpdated);
    }

    [Fact]
    public async Task SetItemStatus_ActualChange_PublishesItemStatus()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new SetItemStatusHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Needed);

        await handler.HandleAsync(boardId, itemId, new SetItemStatusRequest("Completed"), CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.ItemStatus);
    }

    [Fact]
    public async Task SetItemStatus_IdempotentSameStatus_PublishesNothing()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new SetItemStatusHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Completed);

        await handler.HandleAsync(boardId, itemId, new SetItemStatusRequest("Completed"), CancellationToken.None);

        capturing.Published.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteItem_PublishesItemRemoved()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new DeleteItemHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId);

        await handler.HandleAsync(boardId, itemId, CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.ItemRemoved);
    }

    [Fact]
    public async Task ClearCompleted_HasCompletedItems_PublishesItemsClearedWithIds()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new ClearCompletedHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        fixture.WithItem(boardId, "C1", BoardItemStatus.Completed);
        fixture.WithItem(boardId, "C2", BoardItemStatus.Completed);

        await handler.HandleAsync(boardId, CancellationToken.None);

        capturing.Published.Should().ContainSingle();
        capturing.Published[0].Ev.Event.Should().Be(BoardEventNames.ItemsCleared);
    }

    [Fact]
    public async Task ClearCompleted_ZeroCompleted_PublishesNothing()
    {
        var capturing = new CapturingBoardBroadcaster();
        var handler = new ClearCompletedHandler(DbContext, MockCurrentUserService.GetSuccessful().Mock.Object, capturing);
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        fixture.WithItem(boardId, "N1", BoardItemStatus.Needed);

        await handler.HandleAsync(boardId, CancellationToken.None);

        capturing.Published.Should().BeEmpty();
    }

    // Fix 4 — revival from Removed clears RemovedAt

    [Fact]
    public async Task SetItemStatus_RemovedToNeeded_ClearsRemovedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Removed);
        var item = Db.Find<BoardItem>(itemId);
        item!.RemovedAt = DateTime.UtcNow.AddDays(-1);
        await DbContext.SaveChangesAsync();
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Needed"), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Status.Should().Be(BoardItemStatus.Needed);
        updated.RemovedAt.Should().BeNull();
    }

    [Fact]
    public async Task SetItemStatus_RemovedToCompleted_ClearsRemovedAt()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId).WithItem(out var itemId, boardId, status: BoardItemStatus.Removed);
        var item = Db.Find<BoardItem>(itemId);
        item!.RemovedAt = DateTime.UtcNow.AddDays(-1);
        await DbContext.SaveChangesAsync();
        var sut = fixture.SetItemStatus;

        var result = await sut.HandleAsync(boardId, itemId, new SetItemStatusRequest("Completed"), CancellationToken.None);

        AssertSuccess(result);
        var updated = Db.Find<BoardItem>(itemId);
        updated!.Status.Should().Be(BoardItemStatus.Completed);
        updated.RemovedAt.Should().BeNull();
    }
}
