using System.Text.RegularExpressions;

namespace HV_Travel.Web.Tests;

public class ChatKeyboardShortcutTests
{
    [Theory]
    [InlineData(@"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml")]
    [InlineData(@"HV-Travel.Web\Areas\Admin\Views\Messages\Index.cshtml")]
    [InlineData(@"HV-Travel.Web\Areas\Admin\Views\Shared\_AdminMessageCenter.cshtml")]
    public void ChatView_HandlesEnterToSubmitAndShiftEnterToNewLine(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        Assert.Matches(new Regex(@"addEventListener\('keydown'"), content);
        Assert.Matches(new Regex(@"event\.key\s*===\s*'Enter'"), content);
        Assert.Matches(new Regex(@"!event\.shiftKey"), content);
        Assert.Matches(new Regex(@"requestSubmit\(\)"), content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
