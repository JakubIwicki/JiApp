using JiApp.Scheduler.Features.Reports.ClientReport;

namespace JiApp.Scheduler.Tests.Features.Reports.ClientReport;

public sealed class ClientReportValidatorTests
{
    private readonly ClientReportValidator _sut = new();

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var request = new ClientReportRequest(1, "frequency");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBoardIdZero_ReturnsError()
    {
        var request = new ClientReportRequest(0, "frequency");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptySortBy_ReturnsError()
    {
        var request = new ClientReportRequest(1, "");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongSortBy_ReturnsError()
    {
        var request = new ClientReportRequest(1, new string('x', 51));

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidSortBy_ReturnsError()
    {
        var request = new ClientReportRequest(1, "invalid");

        var result = _sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}