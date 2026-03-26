namespace HV_Travel.Web.Tests;

public class AdminDashboardRevenueMarkupTests
{
    [Fact]
    public void DashboardRevenueSection_UsesChartCanvasAndRangeHooks()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Dashboard\Index.cshtml"));

        Assert.Contains("dashboard-revenue-chart", content);
        Assert.Contains("dashboard-revenue-summary", content);
        Assert.Contains("dashboard-revenue-status", content);
        Assert.Contains("data-range=\"week\"", content);
        Assert.Contains("data-range=\"month\"", content);
        Assert.Contains("data-range=\"year\"", content);
        Assert.Contains("https://cdn.jsdelivr.net/npm/chart.js", content);
    }

    [Fact]
    public void DashboardRevenueSection_RemovesStaticMockBars()
    {
        var content = File.ReadAllText(GetRepoPath(@"HV-Travel.Web\Areas\Admin\Views\Dashboard\Index.cshtml"));

        Assert.DoesNotContain("Simplified Mock for Static View", content);
        Assert.DoesNotContain("h-[90%] rounded-t-lg relative group", content);
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
