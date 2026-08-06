using JiApp.Common.Authentication;
using api.JiApp.LovingBoards.Configuration;
using api.JiApp.LovingBoards.Features.Items.CreateItem;
using api.JiApp.LovingBoards.Realtime;
using api.JiApp.LovingBoards.Tests.Realtime;

namespace api.JiApp.LovingBoards.Tests.Features.Items.CreateItem;

public sealed class CreateItemHandlerTests : HandlerTestBase<LovingBoardsDbContext>
{
    private static readonly LovingBoardsSettings DefaultSettings = new()
    {
        ConnectionString = "Data Source=:memory:",
        Jwt = new JwtSettings { Key = "key", Issuer = "iss", Audience = "aud" },
        MaxBoardsPerUser = 3,
        DefaultPageSize = 50,
        MaxBoardNameLength = 200,
        MaxItemsPerBoard = 2,
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
        private readonly IBoardBroadcaster _broadcaster = new NoOpBoardBroadcaster();
        private readonly TimeProvider _timeProvider = TimeProvider.System;

        private Fixture(ILovingBoardsDbContext dbContext, TestDb testDb)
        {
            _dbContext = dbContext;
            _testDb = testDb;
            _currentUser = MockCurrentUserService.GetSuccessful().Mock.Object;
            _settings = DefaultSettings;
        }

        public CreateItemHandler CreateItem => new(_dbContext, _settings, _currentUser, _broadcaster, _timeProvider, new BoardWriteLock());

        public static Fixture Init(ILovingBoardsDbContext dbContext, TestDb testDb) => new(dbContext, testDb);

        public Fixture WithBoard(out long boardId)
        {
            var board = new Board { Name = "Test", OwnerUserId = 1L, MemberUserIds = [1L] };
            _testDb.Store(board);
            boardId = board.Id;
            return this;
        }

        public Fixture WithItems(long boardId, int count)
        {
            for (var i = 0; i < count; i++)
            {
                _testDb.Store(new BoardItem
                {
                    BoardId = boardId,
                    Title = $"Item {i}",
                    Status = BoardItemStatus.Needed,
                    AddedByUserId = 1L
                });
            }
            return this;
        }
    }

    [Fact]
    public async Task CreateItem_UnderCap_ReturnsItemId_AndPersistsItem()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);

        var result = await fixture.CreateItem.HandleAsync(boardId, new CreateItemRequest("Milk"), CancellationToken.None);

        var itemId = AssertSuccess(result);
        var item = Db.Find<BoardItem>(itemId);
        item.Should().NotBeNull();
        item!.Title.Should().Be("Milk");
        item.Status.Should().Be(BoardItemStatus.Needed);
        item.AddedByUserId.Should().Be(1L);
    }

    [Fact]
    public async Task CreateItem_CapReached_ReturnsValidationFailure()
    {
        var fixture = Fixture.Init(DbContext, Db).WithBoard(out var boardId);
        fixture.WithItems(boardId, DefaultSettings.MaxItemsPerBoard);

        var result = await fixture.CreateItem.HandleAsync(boardId, new CreateItemRequest("Over Limit"), CancellationToken.None);

        AssertValidationFailure(result);
        result.Error.Should().Contain("Maximum number of items");
        AssertEntityCount<BoardItem>((LovingBoardsDbContext)DbContext, DefaultSettings.MaxItemsPerBoard);
    }
}
