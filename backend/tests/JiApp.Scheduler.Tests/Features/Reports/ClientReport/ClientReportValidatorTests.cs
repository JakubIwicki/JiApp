using JiApp.Scheduler.Features.Reports.ClientReport;

namespace JiApp.Scheduler.Tests.Features.Reports.ClientReport;

public sealed class ClientReportValidatorTests
{
    private sealed class Fixture
    {
        public ClientReportValidator Sut => new();

        public static Fixture Init() => new();
    }

    [Fact]
    public void Validate_WithValidRequest_IsValid()
    {
        var fixture = Fixture.Init();
        var request = new ClientReportRequest(1, "frequency");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBoardIdZero_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ClientReportRequest(0, "frequency");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptySortBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ClientReportRequest(1, "");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithTooLongSortBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ClientReportRequest(1, new string('x', 51));

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidSortBy_ReturnsError()
    {
        var fixture = Fixture.Init();
        var request = new ClientReportRequest(1, "invalid");

        var result = fixture.Sut.Validate(request);

        result.IsValid.Should().BeFalse();
    }
}
