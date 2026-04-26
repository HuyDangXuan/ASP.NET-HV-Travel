using System.Text.RegularExpressions;

namespace HV_Travel.Web.Tests;

public class AdminEditRichTextCoverageTests
{
    private static readonly string RepoRoot = GetRepoRoot();

    [Fact]
    public void ContentHubEdit_Body_UsesRichTextMarker()
    {
        var markup = ReadAdminEditView("ContentHub", "Edit.cshtml");

        Assert.Matches(new Regex(@"<textarea[^>]*asp-for=""Body""[^>]*data-rich-editor=""true""", RegexOptions.Multiline), markup);
    }

    [Fact]
    public void ContentHubEdit_Summary_RemainsPlainTextarea()
    {
        var markup = ReadAdminEditView("ContentHub", "Edit.cshtml");

        Assert.DoesNotMatch(new Regex(@"<textarea[^>]*asp-for=""Summary""[^>]*data-rich-editor=", RegexOptions.Multiline), markup);
    }

    [Fact]
    public void PromotionsEdit_Description_UsesRichTextMarker()
    {
        var markup = ReadAdminEditView("Promotions", "Edit.cshtml");

        Assert.Matches(new Regex(@"<textarea[^>]*asp-for=""Description""[^>]*data-rich-editor=""true""", RegexOptions.Multiline), markup);
    }

    [Fact]
    public void PromotionsViews_RenderDescription_WithTrustedRichTextFormatter()
    {
        var adminIndexMarkup = ReadAdminEditView("Promotions", "Index.cshtml");
        var publicIndexMarkup = File.ReadAllText(Path.Combine(RepoRoot, "HV-Travel.Web", "Views", "Promotions", "Index.cshtml"));

        Assert.Contains(@"Html.Raw(RichTextContentFormatter.ToTrustedHtml(item.Description))", adminIndexMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("@item.Description", adminIndexMarkup, StringComparison.Ordinal);

        Assert.Contains(@"Html.Raw(RichTextContentFormatter.ToTrustedHtml(promo.Description))", publicIndexMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("@promo.Description", publicIndexMarkup, StringComparison.Ordinal);
    }

    [Fact]
    public void ExistingAdminEditViews_KeepTheirRichTextMarkers()
    {
        var bookingsMarkup = ReadAdminEditView("Bookings", "Edit.cshtml");
        var toursMarkup = ReadAdminEditView("Tours", "Edit.cshtml");

        Assert.Matches(new Regex(@"<textarea[^>]*asp-for=""Notes""[^>]*data-rich-editor=""true""", RegexOptions.Multiline), bookingsMarkup);
        Assert.Matches(new Regex(@"<textarea[^>]*asp-for=""ShortDescription""[^>]*data-rich-editor=""true""", RegexOptions.Multiline), toursMarkup);
        Assert.Matches(new Regex(@"<textarea[^>]*asp-for=""Description""[^>]*data-rich-editor=""true""", RegexOptions.Multiline), toursMarkup);
    }

    private static string ReadAdminEditView(string areaFolder, string viewFileName)
    {
        var viewPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", areaFolder, viewFileName);
        return File.ReadAllText(viewPath);
    }

    private static string GetRepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "HV-Travel.slnx")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find repository root from test output directory.");
    }
}
