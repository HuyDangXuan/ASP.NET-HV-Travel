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
        @"HV-Travel.Web\Views\Home\About.cshtml",
        @"HV-Travel.Web\Views\Home\Contact.cshtml",
        @"HV-Travel.Web\Views\PublicTours\Index.cshtml",
        @"HV-Travel.Web\Views\PublicTours\Details.cshtml",
        @"HV-Travel.Web\Views\Destinations\Index.cshtml",
        @"HV-Travel.Web\Views\Promotions\Index.cshtml",
        @"HV-Travel.Web\Views\Services\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Details.cshtml",
        @"HV-Travel.Web\Views\BookingLookup\Index.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Login.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Register.cshtml",
        @"HV-Travel.Web\Views\CustomerPortal\Index.cshtml",
        @"HV-Travel.Web\Views\Booking\Consultation.cshtml",
        @"HV-Travel.Web\Views\Booking\Create.cshtml",
        @"HV-Travel.Web\Views\Booking\Payment.cshtml",
        @"HV-Travel.Web\Views\Booking\Success.cshtml",
        @"HV-Travel.Web\Views\Booking\Failed.cshtml",
        @"HV-Travel.Web\Views\Booking\Error.cshtml",
        @"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml",
        @"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml",
        @"HV-Travel.Web\wwwroot\js\admin-cloudinary.js"
    };

    private static readonly string[] SuspiciousSequences =
    {
        "Ã",
        "Â",
        "Ä",
        "Æ",
        "áº",
        "á»",
        "â€™",
        "â€œ",
        "â€"
    };

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void File_Should_Not_Contain_Mojibake(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        foreach (var suspiciousSequence in SuspiciousSequences)
        {
            Assert.DoesNotContain(suspiciousSequence, content);
        }
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
