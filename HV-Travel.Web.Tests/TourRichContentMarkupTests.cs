namespace HV_Travel.Web.Tests;

public class TourRichContentMarkupTests
{
    [Fact]
    public void PublicTourDetails_RendersRichHtmlForDescriptionAndSchedule()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\PublicTours\Details.cshtml"));

        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(Model.Description)", content);
        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(item.Description)", content);
        Assert.DoesNotContain("Model.Description?.Replace(\"\\n\", \"<br/>\")", content);
        Assert.DoesNotContain("<p class=\"text-sm text-slate-600 dark:text-slate-400 leading-relaxed mb-3\">@item.Description</p>", content);
    }

    [Fact]
    public void AdminTourDetails_RendersRichHtmlForDescriptionAndSchedule()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Tours\Details.cshtml"));

        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(Model.Description)", content);
        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(Model.ShortDescription)", content);
        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(item.Description)", content);
        Assert.DoesNotContain("@Model.Description", content);
        Assert.DoesNotContain("@item.Description", content);
    }

    [Fact]
    public void PublicLayout_UsesPlainTextSummaryForMetaDescription()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml"));

        Assert.Contains("RichTextContentFormatter.ToPlainTextSummary(ViewData[\"Description\"]?.ToString()", content);
    }

    [Fact]
    public void TourAdminCreateAndEditPreview_StripsHtmlBeforePreviewing()
    {
        var createContent = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml"));
        var editContent = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml"));

        Assert.Contains("function htmlToPreviewText(html)", createContent);
        Assert.Contains("preview.desc.textContent = htmlToPreviewText(inputs.desc.value)", createContent);
        Assert.Contains("function htmlToPreviewText(html)", editContent);
        Assert.Contains("preview.desc.textContent = htmlToPreviewText(inputs.desc.value)", editContent);
    }

    [Fact]
    public void BookingCreate_RendersTourRichHtmlThroughFormatter()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Views\Booking\Create.cshtml"));

        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(tour.ShortDescription)", content);
        Assert.Contains("RichTextContentFormatter.ToTrustedHtml(tour.Description)", content);
        Assert.DoesNotContain("@Html.Raw(tour.ShortDescription)", content);
        Assert.DoesNotContain("@Html.Raw(tour.Description)", content);
    }
    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
