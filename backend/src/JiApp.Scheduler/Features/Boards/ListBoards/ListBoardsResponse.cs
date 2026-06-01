using JiApp.Scheduler.Features.Boards.GetBoard;

namespace JiApp.Scheduler.Features.Boards.ListBoards;

[Serializable]
public sealed record ListBoardsResponse(IReadOnlyList<GetBoardResponse> Boards);