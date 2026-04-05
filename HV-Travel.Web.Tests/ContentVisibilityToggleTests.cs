namespace HV_Travel.Web.Tests;

public class ContentVisibilityToggleTests
{
    [Fact]
    public void ContentModels_Expose_Field_And_Metadata_Visibility_Flags()
    {
        var siteSettings = File.ReadAllText(GetRepoPath(@"HV-Travel.Domain\Entities\SiteSettings.cs"));
        var contentSection = File.ReadAllText(GetRepoPath(@"HV-Travel.Domain\Entities\ContentSection.cs"));

        Assert.Contains("BsonElement(\"isEnabled\")", siteSettings);
        Assert.Contains("public bool IsEnabled { get; set; } = true;", siteSettings);
        Assert.Contains("BsonElement(\"isTitleEnabled\")", siteSettings);
        Assert.Contains("BsonElement(\"isDescriptionEnabled\")", siteSettings);
        Assert.Contains("BsonElement(\"isTitleEnabled\")", contentSection);
        Assert.Contains("BsonElement(\"isDescriptionEnabled\")", contentSection);
    }

    [Fact]
    public void PublicContentDefaults_And_Service_Preserve_Visibility_Flags()
    {
        var defaults = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\PublicContentDefaults.cs"));
        var service = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\PublicContentService.cs"));
        var controller = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Controllers\ContentController.cs"));

        Assert.Contains("Group(\"header\"", defaults);
        Assert.Contains("Section(\"home\", \"hero\"", defaults);
        Assert.Contains("group.IsTitleEnabled = storedGroup.IsTitleEnabled;", service);
        Assert.Contains("group.IsDescriptionEnabled = storedGroup.IsDescriptionEnabled;", service);
        Assert.Contains("field.IsEnabled = storedField.IsEnabled;", service);
        Assert.Contains("field.Style = ContentPresentationSchema.SanitizeTextStyle(storedField.Style);", service);
        Assert.Contains("field.Style = ContentPresentationSchema.SanitizeTextStyle(storedField.Style);", service);
        Assert.Contains("field.IsEnabled = storedField.IsEnabled;", service);
        Assert.Contains("field.Style = ContentPresentationDefaults.CloneTextStyle(storedField.Style);", service);
        Assert.Contains("target.IsTitleEnabled = posted.IsTitleEnabled;", service);
        Assert.Contains("target.IsDescriptionEnabled = posted.IsDescriptionEnabled;", service);
        Assert.Contains("targetField.IsEnabled = postedField.IsEnabled;", service);
        Assert.Contains("IsTitleEnabled = source.IsTitleEnabled", controller);
        Assert.Contains("IsDescriptionEnabled = source.IsDescriptionEnabled", controller);
        Assert.Contains("IsEnabled = field.IsEnabled", controller);
    }

    [Fact]
    public void ContentAccessExtensions_Use_Visibility_Aware_Field_Resolution()
    {
        var helpers = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Services\ContentAccessExtensions.cs"));

        Assert.Contains("GetVisibleFieldValue", helpers);
        Assert.Contains("HasVisibleFieldValue", helpers);
        Assert.Contains("GetVisibleTitle", helpers);
        Assert.Contains("GetVisibleDescription", helpers);
        Assert.Contains("if (!field.IsEnabled)", helpers);
        Assert.Contains("return string.Empty;", helpers);
        Assert.Contains("return GetVisibleFieldValue(fields, key, fallback);", helpers);
    }

    [Fact]
    public void ContentController_Uses_Utf8_Admin_Cms_Feedback_Messages()
    {
        var controller = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Controllers\ContentController.cs"));

        Assert.Contains("Nội Dung Website", controller);
        Assert.Contains("Đã lưu cấu hình site-wide.", controller);
        Assert.Contains("Đã lưu nội dung trang.", controller);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
