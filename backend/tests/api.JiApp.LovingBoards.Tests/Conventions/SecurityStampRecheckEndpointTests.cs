using JiApp.Testing.Common.Conventions;

namespace api.JiApp.LovingBoards.Tests.Conventions;

public sealed class SecurityStampRecheckEndpointTests
{
    [Fact]
    public void AllPrivilegeChangingMutations_CarrySecurityStampRecheckFilter()
    {
        // Every privilege-changing mutation (delete / remove-member / add-member /
        // reset / clear) must recheck the caller's security stamp before acting.
        // Read and plain create/update endpoints are intentionally not listed.
        var required = new HashSet<string>(StringComparer.Ordinal)
        {
            "api.JiApp.LovingBoards.Features.Boards.AddBoardMember.AddBoardMemberEndpoint",
            "api.JiApp.LovingBoards.Features.Boards.DeleteBoard.DeleteBoardEndpoint",
            "api.JiApp.LovingBoards.Features.Boards.RemoveBoardMember.RemoveBoardMemberEndpoint",
            "api.JiApp.LovingBoards.Features.Items.DeleteItem.DeleteItemEndpoint",
            "api.JiApp.LovingBoards.Features.Items.ResetWeeklyItems.ResetWeeklyItemsEndpoint",
            "api.JiApp.LovingBoards.Features.Items.ClearCompleted.ClearCompletedEndpoint"
        };

        var result = EndpointSecurityStampConvention.CollectEndpointsMissingSecurityStampFilter(
            typeof(api.JiApp.LovingBoards.Program).Assembly, required);

        Assert.True(result.ScannedCount == required.Count,
            $"Scanned {result.ScannedCount} of {required.Count} required endpoints — a required endpoint type was not discovered");
        Assert.True(result.Violations.Count == 0,
            $"The following {result.Violations.Count} endpoint(s) lack the SecurityStampRecheckFilter:\n" +
            string.Join("\n", result.Violations));
    }
}
