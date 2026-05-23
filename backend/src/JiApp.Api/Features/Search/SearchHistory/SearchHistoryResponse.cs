using System;
using System.Collections.Generic;

namespace JiApp.Api.Features.Search.SearchHistory;

public sealed record SearchHistoryResponse(IReadOnlyList<SearchHistoryItem> Items);

public sealed record SearchHistoryItem(long Id, string SearchText, DateTime SearchedAt);