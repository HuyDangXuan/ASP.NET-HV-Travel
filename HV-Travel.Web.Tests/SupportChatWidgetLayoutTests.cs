namespace HV_Travel.Web.Tests;

public class SupportChatWidgetLayoutTests
{
    [Fact]
    public void GuestChat_UsesSeparateOnboardingAndConversationStates()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.Contains("support-chat-onboarding-step", content);
        Assert.Contains("support-chat-conversation-step", content);
        Assert.DoesNotContain("support-chat-profile-card", content);
        Assert.DoesNotContain("support-chat-chat-step", content);
        Assert.Contains("support-chat-conversation-mode", content);
    }

    [Fact]
    public void ActiveConversation_PrioritizesThreadOverDecorativeChrome()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.Contains("support-chat-messages", content);
        Assert.Contains("support-chat-thread", content);
        Assert.Contains("public-chat-thread", content);
        Assert.Contains("public-chat-bubble", content);
        Assert.DoesNotContain("public-chat-conversation-header", content);
        Assert.DoesNotContain("hero-gradient shrink-0 px-5 py-5 text-white", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}




