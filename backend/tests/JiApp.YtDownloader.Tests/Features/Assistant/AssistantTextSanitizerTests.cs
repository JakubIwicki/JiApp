using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class AssistantTextSanitizerTests
{
    [Fact]
    public void ProcessDelta_WithCleanText_PassesThroughUnchanged()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("Here are songs: lofi beats");

        result.Should().Be("Here are songs: lofi beats");
    }

    [Fact]
    public void ProcessDelta_WithToolCallMarkup_StripsMarkupInSingleDelta()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta(
            "Here are songs:<|invoke name=\"offer_download\"><|parameter name=\"videoId\" string=\"true\">GJRIs8VqDPg</|parameter>");

        result.Should().Be("Here are songs:");
    }

    [Fact]
    public void ProcessDelta_AfterMarkerDetected_SuppressesAllSubsequentDeltas()
    {
        var sut = new AssistantTextSanitizer();

        var first = sut.ProcessDelta("Clean start. <|tool_calls>");
        var second = sut.ProcessDelta("more markup here");
        var third = sut.ProcessDelta("even more");

        first.Should().Be("Clean start. ");
        second.Should().BeNull();
        third.Should().BeNull();
    }

    [Fact]
    public void ProcessDelta_WithMarkerSplitAcrossTwoDeltas_HandlesCorrectly()
    {
        var sut = new AssistantTextSanitizer();

        var first = sut.ProcessDelta("Here are songs: <|inv");
        var second = sut.ProcessDelta("oke name=\"offer_download\"><|parameter>data</|parameter>");

        first.Should().Be("Here are songs: ");
        second.Should().BeNull();
    }

    [Fact]
    public void ProcessDelta_WithFullwidthTokenVariant_StripsMarkup()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("Found something<｜tool▁calls▁begin｜>extra markup");

        result.Should().Be("Found something");
    }

    [Fact]
    public void ProcessDelta_WithLoneLessThanSign_PassesThrough()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("2 < 3 and 5 > 3");

        result.Should().Be("2 < 3 and 5 > 3");
    }

    [Fact]
    public void ProcessDelta_WithLessThanFollowedBySpace_IsSafe()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("the symbol < is less than");

        result.Should().Be("the symbol < is less than");
    }

    [Fact]
    public void ProcessDelta_WithMultipleCleanDeltas_AccumulatesCorrectly()
    {
        var sut = new AssistantTextSanitizer();

        var first = sut.ProcessDelta("Hello ");
        var second = sut.ProcessDelta("world! ");
        var third = sut.ProcessDelta("These are songs.");

        first.Should().Be("Hello ");
        second.Should().Be("world! ");
        third.Should().Be("These are songs.");
    }

    [Fact]
    public void ProcessDelta_WithToolCallsMarkerWithoutBrackets_IsDetected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("some text tool_calls begin more");

        result.Should().Be("some text ");
    }

    [Fact]
    public void ProcessDelta_WithInvokeMarkerWithoutPipe_IsDetected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("text <invoke name=\"search\">");

        result.Should().Be("text ");
    }

    [Fact]
    public void ProcessDelta_WithParameterNameEqualsMarker_IsDetected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("data<parameter name=\"query\" string=\"true\">lofi");

        result.Should().Be("data");
    }

    [Fact]
    public void ProcessDelta_WithMarkerAtStartOfDelta_ReturnsNull()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("<|tool_calls>markup here");

        result.Should().BeNull();
    }

    [Fact]
    public void ProcessDelta_WithBufferFlushes_WhenTailBecomesSafe()
    {
        var sut = new AssistantTextSanitizer();

        var first = sut.ProcessDelta("start <|");
        first.Should().Be("start ");

        var second = sut.ProcessDelta(" actually means something");
        second.Should().Be("<| actually means something");
    }
}
