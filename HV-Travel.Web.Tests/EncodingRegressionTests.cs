namespace HV_Travel.Web.Tests;

public class EncodingRegressionTests
{
    private static readonly string[] FilesToCheck =
    {
        @"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Payments\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Shared\_Layout.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Details.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml",
        @"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml",
        @"HV-Travel.Web\wwwroot\js\admin-cloudinary.js"
    };

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void File_Should_Not_Contain_Mojibake(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        Assert.DoesNotMatch("(Ã.|Â.|Ä.|Æ.|áº.|á».|â€™|â€œ|â€)", content);
    }

    public static IEnumerable<object[]> GetFiles()
    {
        return FilesToCheck.Select(static path => new object[] { path });
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
