using JiApp.Testing.Common.Conventions;

namespace JiApp.Scheduler.Tests.Conventions;

public sealed class SecurityStampRecheckEndpointTests
{
    [Fact]
    public void AllPrivilegeChangingMutations_CarrySecurityStampRecheckFilter()
    {
        // Every privilege-changing mutation (delete / remove-member / add-member) must
        // recheck the caller's security stamp before acting. Read and plain
        // create/update endpoints are intentionally not listed.
        var required = new HashSet<string>(StringComparer.Ordinal)
        {
            "JiApp.Scheduler.Features.Boards.AddBoardMember.AddBoardMemberEndpoint",
            "JiApp.Scheduler.Features.Boards.DeleteBoard.DeleteBoardEndpoint",
            "JiApp.Scheduler.Features.Boards.RemoveBoardMember.RemoveBoardMemberEndpoint",
            "JiApp.Scheduler.Features.Clients.DeleteClient.DeleteClientEndpoint",
            "JiApp.Scheduler.Features.Appointments.DeleteAppointment.DeleteAppointmentEndpoint",
            "JiApp.Scheduler.Features.Services.DeleteService.DeleteServiceEndpoint",
            "JiApp.Scheduler.Features.Expenses.DeleteExpense.DeleteExpenseEndpoint"
        };

        var result = EndpointSecurityStampConvention.CollectEndpointsMissingSecurityStampFilter(
            typeof(JiApp.Scheduler.Program).Assembly, required);

        Assert.True(result.ScannedCount == required.Count,
            $"Scanned {result.ScannedCount} of {required.Count} required endpoints — a required endpoint type was not discovered");
        Assert.True(result.Violations.Count == 0,
            $"The following {result.Violations.Count} endpoint(s) lack the SecurityStampRecheckFilter:\n" +
            string.Join("\n", result.Violations));
    }
}
