using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class AssistantTextSanitizerTests
{
    [Fact]
    public void ProcessDelta_clean_text_passes_through_unchanged()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("Here are songs: lofi beats");

        result.Should().Be("Here are songs: lofi beats");
    }

    [Fact]
    public void ProcessDelta_strips_tool_call_markup_when_present_in_single_delta()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta(
            "Here are songs:<|invoke name=\"offer_download\"><|parameter name=\"videoId\" string=\"true\">GJRIs8VqDPg</|parameter>");

        result.Should().Be("Here are songs:");
    }

    [Fact]
    public void ProcessDelta_suppresses_all_subsequent_deltas_after_marker_is_detected()
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
    public void ProcessDelta_handles_marker_split_across_two_deltas()
    {
        var sut = new AssistantTextSanitizer();

        var first = sut.ProcessDelta("Here are songs: <|inv");
        var second = sut.ProcessDelta("oke name=\"offer_download\"><|parameter>data</|parameter>");

        first.Should().Be("Here are songs: ");
        second.Should().BeNull();
    }

    [Fact]
    public void ProcessDelta_strips_fullwidth_token_variant()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("Found something<｜tool▁calls▁begin｜>extra markup");

        result.Should().Be("Found something");
    }

    [Fact]
    public void ProcessDelta_lone_less_than_sign_passes_through()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("2 < 3 and 5 > 3");

        result.Should().Be("2 < 3 and 5 > 3");
    }

    [Fact]
    public void ProcessDelta_less_than_followed_by_space_is_safe()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("the symbol < is less than");

        result.Should().Be("the symbol < is less than");
    }

    [Fact]
    public void ProcessDelta_multiple_clean_deltas_accumulate_correctly()
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
    public void ProcessDelta_tool_calls_marker_without_brackets_is_detected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("some text tool_calls begin more");

        result.Should().Be("some text ");
    }

    [Fact]
    public void ProcessDelta_invoke_marker_without_pipe_is_detected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("text <invoke name=\"search\">");

        result.Should().Be("text ");
    }

    [Fact]
    public void ProcessDelta_parameter_name_equals_marker_is_detected()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("data<parameter name=\"query\" string=\"true\">lofi");

        result.Should().Be("data");
    }

    [Fact]
    public void ProcessDelta_marker_found_at_start_of_delta_returns_null()
    {
        var sut = new AssistantTextSanitizer();

        var result = sut.ProcessDelta("<|tool_calls>markup here");

        result.Should().BeNull();
    }

    [Fact]
    public void ProcessDelta_buffer_flushes_when_tail_becomes_safe()
    {
        var sut = new AssistantTextSanitizer();

        // "<|" alone looks like a marker prefix, so it's held back
        var first = sut.ProcessDelta("start <|");
        first.Should().Be("start ");

        // Now the next delta confirms it's safe (not a marker)
        var second = sut.ProcessDelta(" actually means something");
        second.Should().Be("<| actually means something");
    }
}
