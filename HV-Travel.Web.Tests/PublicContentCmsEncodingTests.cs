namespace HV_Travel.Web.Tests;

public class PublicContentCmsEncodingTests
{
    private static readonly string[] FilesToCheck =
    {
        @"HV-Travel.Application\Services\CheckoutService.cs",
        @"HV-Travel.Infrastructure\Data\DbInitializer.cs",
        @"HV-Travel.Infrastructure\Repositories\Repository.cs",
        @"HV-Travel.Web\Areas\Admin\Controllers\ContentController.cs",
        @"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml",
        @"HV-Travel.Web\Controllers\BookingController.cs",
        @"HV-Travel.Web\Controllers\BookingLookupController.cs",
        @"HV-Travel.Web\Controllers\CustomerAuthController.cs",
        @"HV-Travel.Web\Controllers\InspirationController.cs",
        @"HV-Travel.Web\Controllers\PublicToursController.cs",
        @"HV-Travel.Web\Models\ContactViewModel.cs",
        @"HV-Travel.Web\Services\ContentAdminCatalog.cs",
        @"HV-Travel.Web\Services\PublicContentDefaults.cs",
        @"HV-Travel.Web\Services\RichTextContentFormatter.cs",
        @"HV-Travel.Web\Views\BookingLookup\Index.cshtml",
        @"HV-Travel.Web\Views\Booking\Create.cshtml",
        @"HV-Travel.Web\Views\Booking\Payment.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Login.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Register.cshtml",
        @"HV-Travel.Web\Views\CustomerPortal\Index.cshtml",
        @"HV-Travel.Web\Views\Destinations\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Details.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
        @"HV-Travel.Web\Views\Promotions\Index.cshtml",
        @"HV-Travel.Web\Views\PublicTours\Details.cshtml",
        @"HV-Travel.Web\Views\Services\Index.cshtml",
        @"HV-Travel.Web.Tests\RichTextContentFormatterTests.cs",
        @"HV-Travel.Web.Tests\TextEncodingRepairTests.cs"
    };

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void CmsCoverageFiles_Should_Not_Contain_Mojibake(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        Assert.DoesNotMatch("(\u00C3.|\u00C2.|\u00C4.|\u00C6.|\u00E1\u00BA.|\u00E1\u00BB.|\u00E2\u20AC\u2122|\u00E2\u20AC\u0153|\u00E2\u20AC\u009D)", content);
        Assert.DoesNotContain('�', content);
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
