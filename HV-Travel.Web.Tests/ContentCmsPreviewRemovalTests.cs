using HVTravel.Web.Models;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class ContentCmsPreviewRemovalTests
{
    [Fact]
    public void ContentController_RemovesPreviewEndpointsAndHelpers()
    {
        var content = ReadRepoFileOrEmpty(@"HV-Travel.Web\Areas\Admin\Controllers\ContentController.cs");

        Assert.DoesNotContain("PreviewSiteSettings", content);
        Assert.DoesNotContain("PreviewSections", content);
        Assert.DoesNotContain("BuildPreviewUrl", content);
        Assert.DoesNotContain("previewSessionKey", content);
        Assert.DoesNotContain("ContentPreview", content);
    }

    [Fact]
    public void ContentCmsView_RemovesInlinePreviewMarkupAndClientHooks()
    {
        var content = ReadRepoFileOrEmpty(@"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml");

        Assert.DoesNotContain("content-live-preview-frame", content);
        Assert.DoesNotContain("data-preview-endpoint", content);
        Assert.DoesNotContain("data-preview-session-key", content);
        Assert.DoesNotContain("data-preview-enabled", content);
        Assert.DoesNotContain("live-preview-refresh", content);
        Assert.DoesNotContain("live-preview-open", content);
        Assert.DoesNotContain("preview-diagnostics", content);
        Assert.DoesNotContain("Preview sẽ tự đồng bộ", content);
        Assert.DoesNotContain("Live preview", content);
    }

    [Fact]
    public void PreviewContracts_AreRemoved_FromViewModelsAndServices()
    {
        var managementProperties = typeof(ContentManagementViewModel)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("PreviewSessionKey", managementProperties);
        Assert.DoesNotContain("LivePreviewUrl", managementProperties);
        Assert.DoesNotContain("CanInlinePreview", managementProperties);
        Assert.DoesNotContain("PreviewTarget", managementProperties);
        Assert.DoesNotContain("PreviewUrl", managementProperties);
        Assert.DoesNotContain("PreviewUnavailableReason", managementProperties);

        var serviceMethods = typeof(IPublicContentService)
            .GetMethods()
            .Select(method => method.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("BuildPreviewSnapshotAsync", serviceMethods);
        Assert.DoesNotContain("StorePreviewSnapshotAsync", serviceMethods);
        Assert.DoesNotContain("GetPreviewSnapshotAsync", serviceMethods);

        var editorProperties = typeof(ContentAdminEditorDefinition)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.DoesNotContain("PreviewTarget", editorProperties);
        Assert.DoesNotContain("PreviewUnavailableReason", editorProperties);

        var previewModelsContent = ReadRepoFileOrEmpty(@"HV-Travel.Web\Models\ContentPreviewModels.cs");
        Assert.DoesNotContain("ContentPreviewConstants", previewModelsContent);
        Assert.DoesNotContain("ContentPreviewSnapshot", previewModelsContent);

        var publicContentServiceContent = ReadRepoFileOrEmpty(@"HV-Travel.Web\Services\PublicContentService.cs");
        Assert.DoesNotContain("ApplyPreviewSections", publicContentServiceContent);
        Assert.DoesNotContain("contentPreviewToken", publicContentServiceContent);
    }

    private static string ReadRepoFileOrEmpty(string relativePath)
    {
        var path = GetRepoPath(relativePath);
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
