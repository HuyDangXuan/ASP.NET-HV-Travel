namespace HV_Travel.Web.Tests;

public class AdminCloudinaryAssetBrowserMarkupTests
{
    [Fact]
    public void AdminLayout_UsesCustomAssetBrowserConfigInsteadOfMediaLibraryScript()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Shared\_Layout.cshtml"));

        Assert.DoesNotContain("https://media-library.cloudinary.com/global/all.js", content);
        Assert.DoesNotContain("data-cloudinary-api-key", content);
        Assert.DoesNotContain("apiKey: \"@Configuration[\"Cloudinary:ApiKey\"]\"", content);
        Assert.Contains("data-cloudinary-assets-url", content);
        Assert.Contains("assetsUrl: \"@Url.Action(\"Assets\", \"Cloudinary\", new { area = \"Admin\" })\"", content);
    }

    [Fact]
    public void AdminCloudinaryScript_SupportsCustomAssetBrowserForExistingAssets()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\admin-cloudinary.js"));

        Assert.Contains("window.openCloudinaryAssetBrowser", content);
        Assert.Contains("selectedUrls", content);
        Assert.Contains("initialSelectedUrls", content);
        Assert.Contains("syncSelection", content);
        Assert.Contains("hasLibrarySelectionChanges", content);
        Assert.Contains("data-cloudinary-library-confirm", content);
        Assert.Contains("if (modalState.options.syncSelection)", content);
        Assert.Contains("data-cloudinary-tab=\"library\"", content);
        Assert.Contains("data-cloudinary-library-grid", content);
        Assert.Contains("fetch(config.assetsUrl", content);
        Assert.DoesNotContain("window.openCloudinaryMediaLibrary", content);
        Assert.DoesNotContain("createMediaLibrary", content);
    }

    [Fact]
    public void TourForms_UseCustomAssetBrowser()
    {
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "tour-images-browse-library");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "tour-images-browse-library");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "window.openCloudinaryAssetBrowser");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "window.openCloudinaryAssetBrowser");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "HV-Travel ASP.NET");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "HV-Travel ASP.NET");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "getCurrentTourImageUrls");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "getCurrentTourImageUrls");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "syncTourImages");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "syncTourImages");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "selectedUrls: getCurrentTourImageUrls()");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "selectedUrls: getCurrentTourImageUrls()");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "syncSelection: true");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "syncSelection: true");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "window.openCloudinaryMediaLibrary");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "window.openCloudinaryMediaLibrary");
    }
    [Fact]
    public void TourForms_SupportDragAndDropImageReordering()
    {
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "image-drag-handle");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "image-drag-handle");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "enableImageReordering");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "enableImageReordering");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "reorderImageItem");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "reorderImageItem");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "image-reorder-placeholder");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "image-reorder-placeholder");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "startImagePointerDrag");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "startImagePointerDrag");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "setPointerCapture");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "setPointerCapture");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml", "draggable=\"true\"");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml", "draggable=\"true\"");
    }

    [Fact]
    public void AvatarForms_UseCustomAssetBrowser()
    {
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml", "user-avatar-browse-library");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml", "user-avatar-browse-library");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml", "customer-avatar-browse-library");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml", "window.openCloudinaryAssetBrowser");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml", "window.openCloudinaryAssetBrowser");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml", "window.openCloudinaryAssetBrowser");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml", "HV-Travel ASP.NET");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml", "HV-Travel ASP.NET");
        AssertContains(@"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml", "HV-Travel ASP.NET");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml", "window.openCloudinaryMediaLibrary");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml", "window.openCloudinaryMediaLibrary");
        AssertDoesNotContain(@"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml", "window.openCloudinaryMediaLibrary");
    }

    private static void AssertContains(string relativePath, string expectedContent)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));
        Assert.Contains(expectedContent, content);
    }

    private static void AssertDoesNotContain(string relativePath, string unexpectedContent)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));
        Assert.DoesNotContain(unexpectedContent, content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
