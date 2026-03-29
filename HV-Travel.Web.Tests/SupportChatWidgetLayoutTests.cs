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


    [Fact]
    public void SupportChat_UsesSemanticHooksForDarkSensitiveSurfaces()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.Contains("public-chat-header", content);
        Assert.Contains("public-chat-close-button", content);
        Assert.Contains("public-chat-profile-card", content);
        Assert.Contains("public-chat-field-label", content);
        Assert.Contains("public-chat-input", content);
        Assert.Contains("public-chat-status-box", content);
        Assert.Contains("public-chat-thread-surface", content);
        Assert.Contains("public-chat-input-shell", content);
        Assert.Contains("public-chat-toggle", content);
        Assert.DoesNotContain("bg-white/95", content);
        Assert.DoesNotContain("bg-slate-50/80", content);
    }

    [Fact]
    public void SupportChat_ScriptUsesSemanticStateClassesForStatusAndBubbles()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml"));

        Assert.Contains("public-chat-status-box is-neutral", content);
        Assert.Contains("public-chat-status-box is-success", content);
        Assert.Contains("public-chat-status-box is-error", content);
        Assert.Contains("public-chat-bubble is-empty", content);
        Assert.Contains("public-chat-bubble is-staff", content);
        Assert.Contains("public-chat-bubble is-system", content);
        Assert.Contains("public-chat-bubble is-customer", content);
        Assert.Contains("public-chat-metadata is-staff", content);
        Assert.Contains("public-chat-metadata is-system", content);
        Assert.Contains("public-chat-timestamp", content);
        Assert.DoesNotContain("border-slate-200 bg-slate-50 text-slate-600", content);
        Assert.DoesNotContain("bg-white text-slate-900", content);
    }

    [Fact]
    public void SupportChat_ThemeDefinesDedicatedDarkModeRules()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\theme-ocean.css"));

        Assert.Contains("html.dark body.public-page-shell .public-chat-panel", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-header", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-onboarding", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-profile-card", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-thread-surface", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-input-shell", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-toggle", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-status-box.is-neutral", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-bubble.is-staff", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-bubble.is-system", content);
        Assert.Contains("html.dark body.public-page-shell .public-chat-timestamp", content);
    }
    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}






