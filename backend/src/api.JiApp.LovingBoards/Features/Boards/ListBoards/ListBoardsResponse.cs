using api.JiApp.LovingBoards.Features.Boards.GetBoard;

namespace api.JiApp.LovingBoards.Features.Boards.ListBoards;

[Serializable]
public sealed record ListBoardsResponse(IReadOnlyList<GetBoardResponse> Boards, bool HasMore);
