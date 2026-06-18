using JiApp.Scheduler.Features.Reports.RevenueReport;

namespace JiApp.Scheduler.Tests.Features.Reports.RevenueReport;

public sealed class RevenueReportValidatorTests
{
    private sealed class Fixture
    {
        public RevenueReportValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "weekend");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBoardIdZero_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new RevenueReportRequest(0, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "weekend");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyGroupBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongGroupBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request =
            new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), new string('x', 51));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidGroupBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new RevenueReportRequest(1, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), "invalid");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
