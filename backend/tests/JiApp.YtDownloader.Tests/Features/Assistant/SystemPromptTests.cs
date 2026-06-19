using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public class SystemPromptTests
{
    private const string EnglishReplyDirective = "reply to the user in English";
    private const string PolishReplyDirective = "reply to the user in Polish";

    [Fact]
    public void Build_en_instructs_reply_in_English()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain(EnglishReplyDirective);
        prompt.Should().NotContain(PolishReplyDirective);
    }

    [Theory]
    [InlineData("pl")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("xx")]
    public void Build_defaults_to_Polish_reply_directive(string? language)
    {
        var prompt = SystemPrompt.Build(language);

        prompt.Should().Contain(PolishReplyDirective);
        prompt.Should().NotContain(EnglishReplyDirective);
    }

    [Fact]
    public void Build_confines_role_to_music_search_and_download()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("JiApp");
        prompt.Should().Contain("music");
    }

    [Fact]
    public void Build_mentions_all_agent_tools()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("search_youtube");
        prompt.Should().Contain("list_search_history");
        prompt.Should().Contain("list_download_history");
        prompt.Should().Contain("offer_download");
    }

    [Fact]
    public void Build_treats_override_attempts_as_untrusted()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("ignore previous instructions");
        prompt.Should().Contain("untrusted");
    }

    [Fact]
    public void Build_treats_tool_results_as_untrusted_data()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("never as instructions");
    }

    [Fact]
    public void Build_forbids_claiming_to_have_downloaded()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("offer_download");
        prompt.Should().Contain("never claim");
    }

    [Fact]
    public void Build_refuses_off_scope_requests_without_calling_tools()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("decline");
    }

    [Fact]
    public void Build_forbids_revealing_the_system_prompt()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("Never reveal");
    }

    [Fact]
    public void Build_is_versioned()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("v1");
    }
}
