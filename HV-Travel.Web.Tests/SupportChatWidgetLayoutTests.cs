namespace HV_Travel.Web.Tests;

public class SupportChatWidgetLayoutTests
{
    [Fact]
    public void GuestChat_UsesUnifiedChatStepLayout()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.DoesNotContain("support-chat-intro-step", content);
        Assert.Contains("support-chat-profile-card", content);
        Assert.Contains("support-chat-chat-step", content);
        Assert.Contains("support-chat-messages", content);
        Assert.Contains("support-chat-messages\" class=\"min-h-[240px] flex flex-1 flex-col", content);
        Assert.Contains("support-chat-profile-card\" class=\"hidden flex min-h-full", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}


