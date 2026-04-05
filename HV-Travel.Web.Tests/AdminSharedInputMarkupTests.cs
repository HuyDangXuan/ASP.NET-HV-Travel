namespace HV_Travel.Web.Tests;

public class AdminSharedInputMarkupTests
{
    [Fact]
    public void AdminSelectForms_UseSharedSelectHooks()
    {
        var usersCreate = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml"));
        var serviceLeads = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\ServiceLeads\Index.cshtml"));
        var selectScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-custom-select.js"));
        var selectStyles = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\admin-custom-select.css"));

        Assert.Contains("admin-shared-select", usersCreate);
        Assert.Contains("data-public-select-id=\"adminUserRole\"", usersCreate);
        Assert.Contains("~/js/public-custom-select.js", usersCreate);
        Assert.Contains("admin-shared-select", serviceLeads);
        Assert.Contains("var selectId = $\"leadStatus-{lead.Id}\";", serviceLeads);
        Assert.Contains("data-public-select-id=\"@selectId\"", serviceLeads);
        Assert.Contains("~/js/public-custom-select.js", serviceLeads);
        Assert.Contains("data-admin-select-mode", selectScript);
        Assert.Contains(".admin-shared-select", selectStyles);
        Assert.Contains(".admin-shared-select-trigger", selectStyles);
    }

    [Fact]
    public void AdminDateTimeForms_UseSharedDatePickerHooks()
    {
        var promotionsCreate = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Promotions\Create.cshtml"));
        var promotionsEdit = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Promotions\Edit.cshtml"));
        var contentCreate = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\ContentHub\Create.cshtml"));
        var contentEdit = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\ContentHub\Edit.cshtml"));
        var dateScript = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\js\public-date-picker.js"));
        var dateStyles = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\wwwroot\css\admin-tour-date-picker.css"));

        foreach (var content in new[] { promotionsCreate, promotionsEdit, contentCreate, contentEdit })
        {
            Assert.Contains("admin-date-input-shell", content);
            Assert.Contains("data-public-date-input", content);
            Assert.Contains("data-public-datetime-input", content);
            Assert.Contains("type=\"time\"", content);
            Assert.Contains("~/js/public-date-picker.js", content);
        }

        Assert.Contains("data-public-datetime-input", dateScript);
        Assert.Contains("data-public-time-input", dateScript);
        Assert.Contains("combineDateAndTime", dateScript);
        Assert.Contains(".admin-date-input-shell", dateStyles);
        Assert.Contains(".admin-date-time-input", dateStyles);
        Assert.DoesNotContain("@Html.AntiForgeryToken()`r`n", promotionsCreate);
        Assert.DoesNotContain("@Html.AntiForgeryToken()`r`n", contentCreate);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}