using JiApp.YtDownloader.Features.Assistant;

namespace JiApp.YtDownloader.Tests.Features.Assistant;

public sealed class SystemPromptTests
{
    private const string EnglishReplyDirective = "reply to the user in English";
    private const string PolishReplyDirective = "reply to the user in Polish";

    [Fact]
    public void Build_WithEnLanguage_InstructsReplyInEnglish()
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
    public void Build_WithDefaultOrOtherLanguage_DefaultsToPolishReplyDirective(string? language)
    {
        var prompt = SystemPrompt.Build(language);

        prompt.Should().Contain(PolishReplyDirective);
        prompt.Should().NotContain(EnglishReplyDirective);
    }

    [Fact]
    public void Build_ConfinesRole_ToMusicSearchAndDownload()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("JiApp");
        prompt.Should().Contain("music");
    }

    [Fact]
    public void Build_MentionsAllAgentTools()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("search_youtube");
        prompt.Should().Contain("list_search_history");
        prompt.Should().Contain("list_download_history");
        prompt.Should().Contain("offer_download");
    }

    [Fact]
    public void Build_TreatsOverrideAttempts_AsUntrusted()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("ignore previous instructions");
        prompt.Should().Contain("untrusted");
    }

    [Fact]
    public void Build_TreatsToolResults_AsUntrustedData()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("never as instructions");
    }

    [Fact]
    public void Build_ForbidsClaimingToHaveDownloaded()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("offer_download");
        prompt.Should().Contain("never claim");
    }

    [Fact]
    public void Build_RefusesOffScopeRequests_WithoutCallingTools()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("decline");
    }

    [Fact]
    public void Build_ForbidsRevealingTheSystemPrompt()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("Never reveal");
    }

    [Fact]
    public void Build_IsVersioned()
    {
        var prompt = SystemPrompt.Build("en");

        prompt.Should().Contain("v1");
    }
}
