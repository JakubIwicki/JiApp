using JiApp.Scheduler.Features.Reports.RevenueReport;

namespace JiApp.Scheduler.Tests.Features.Reports.RevenueReport;

public sealed class RevenueReportValidatorTests
{
    private readonly RevenueReportValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "weekend");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBoardIdZero_ReturnsError()
    {
        var request = new RevenueReportRequest(0, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "weekend");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyGroupBy_ReturnsError()
    {
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongGroupBy_ReturnsError()
    {
        var request =
            new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), new string('x', 51));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidGroupBy_ReturnsError()
    {
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "invalid");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}