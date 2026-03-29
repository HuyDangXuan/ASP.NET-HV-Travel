using System.IO;

namespace HV_Travel.Web.Tests;

public class AdminRichTextSelfHostTests
{
    [Fact]
    public void AdminLayout_LoadsLocalTinyMceScriptInsteadOfCloudCdn()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Shared\_Layout.cshtml"));

        Assert.DoesNotContain("cdn.tiny.cloud", content);
        Assert.Contains("~/lib/tinymce/tinymce.min.js", content);
        Assert.Contains("~/js/admin-rich-text.js", content);
    }

    [Fact]
    public void AdminRichTextScript_UsesGplLicenseAndReadableVietnameseBlockFormats()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\admin-rich-text.js"));

        Assert.Contains("license_key: 'gpl'", content);
        Assert.Contains("block_formats: '\\u0110o\\u1ea1n v\\u0103n=p; Ti\\u00eau \\u0111\\u1ec1 2=h2; Ti\\u00eau \\u0111\\u1ec1 3=h3; Ti\\u00eau \\u0111\\u1ec1 4=h4'", content);
        Assert.DoesNotContain("Äoáº¡n", content);
        Assert.Contains("plugins: 'autolink link lists table code preview searchreplace visualblocks wordcount autoresize'", content);
        Assert.Contains("toolbar: 'undo redo | blocks | fontfamily fontsizeinput | bold italic underline strikethrough | forecolor backcolor | bullist numlist | alignleft aligncenter alignright alignjustify | blockquote table link | removeformat code preview'", content);
        Assert.Contains("font_size_input_default_unit: 'px'", content);
        Assert.DoesNotContain("fontfamily fontsize |", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
