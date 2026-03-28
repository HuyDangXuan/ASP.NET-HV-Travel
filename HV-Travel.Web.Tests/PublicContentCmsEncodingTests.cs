namespace HV_Travel.Web.Tests;

public class PublicContentCmsEncodingTests
{
    private static readonly string[] FilesToCheck =
    {
        @"HV-Travel.Web\Areas\Admin\Controllers\ContentController.cs",
        @"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml",
        @"HV-Travel.Web\Controllers\BookingLookupController.cs",
        @"HV-Travel.Web\Services\ContentAdminCatalog.cs",
        @"HV-Travel.Web\Services\PublicContentDefaults.cs",
        @"HV-Travel.Web\Views\BookingLookup\Index.cshtml",
        @"HV-Travel.Web\Views\Destinations\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
        @"HV-Travel.Web\Views\Promotions\Index.cshtml",
        @"HV-Travel.Web\Views\Services\Index.cshtml"
    };

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void CmsCoverageFiles_Should_Not_Contain_Mojibake(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        Assert.DoesNotMatch("(\u00C3.|\u00C2.|\u00C4.|\u00C6.|\u00E1\u00BA.|\u00E1\u00BB.|\u00E2\u20AC\u2122|\u00E2\u20AC\u0153|\u00E2\u20AC\u009D)", content);
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
