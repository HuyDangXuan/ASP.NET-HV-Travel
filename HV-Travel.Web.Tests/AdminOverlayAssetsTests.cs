using System.Text.RegularExpressions;

namespace HV_Travel.Web.Tests;

public class AdminOverlayAssetsTests
{
    private static readonly string RepoRoot = GetRepoRoot();

    [Fact]
    public void SharedFloatingLayerScript_Exists()
    {
        var scriptPath = Path.Combine(RepoRoot, "HV-Travel.Web", "wwwroot", "js", "admin-floating-layer.js");

        Assert.True(File.Exists(scriptPath), "Expected a shared admin floating layer script.");
    }

    [Fact]
    public void AdminLayout_LoadsSharedFloatingLayerScript_BeforeOverlayConsumers()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        var floatingLayerIndex = layout.IndexOf("~/js/admin-floating-layer.js", StringComparison.Ordinal);
        var customSelectIndex = layout.IndexOf("~/js/admin-custom-select.js", StringComparison.Ordinal);
        var dateTimePickerIndex = layout.IndexOf("~/js/admin-date-time-picker.js", StringComparison.Ordinal);

        Assert.True(floatingLayerIndex >= 0, "Expected admin layout to load admin-floating-layer.js.");
        Assert.True(customSelectIndex > floatingLayerIndex, "Expected admin-custom-select.js to load after admin-floating-layer.js.");
        Assert.True(dateTimePickerIndex > floatingLayerIndex, "Expected admin-date-time-picker.js to load after admin-floating-layer.js.");
    }

    [Fact]
    public void AdminCustomSelect_UsesSharedFloatingLayer()
    {
        var scriptPath = Path.Combine(RepoRoot, "HV-Travel.Web", "wwwroot", "js", "admin-custom-select.js");
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("function createFloatingLayerApi()", script, StringComparison.Ordinal);
        Assert.Contains("window.AdminFloatingLayer", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminDateTimePicker_UsesSharedFloatingLayer()
    {
        var scriptPath = Path.Combine(RepoRoot, "HV-Travel.Web", "wwwroot", "js", "admin-date-time-picker.js");
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("function createFloatingLayerApi()", script, StringComparison.Ordinal);
        Assert.Contains("window.AdminFloatingLayer", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminLayout_MenuApi_UsesSharedFloatingLayer()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.Matches(new Regex(@"window\.AdminMenu\s*=\s*\{[\s\S]*window\.AdminFloatingLayer", RegexOptions.Multiline), layout);
    }

    [Fact]
    public void AdminLayout_OpenMenu_ResolvesAnchorBeforeAttachingToFloatingLayer()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        var openMenuIndex = layout.IndexOf("function openMenu(menu)", StringComparison.Ordinal);
        var anchorIndex = layout.IndexOf("const anchor = inferMenuAnchor(menu);", openMenuIndex, StringComparison.Ordinal);
        var guardIndex = layout.IndexOf("if (!menu || !floatingLayer || !anchor || !document.body.contains(anchor))", openMenuIndex, StringComparison.Ordinal);
        var attachIndex = layout.IndexOf("floatingLayer.attach(menu);", openMenuIndex, StringComparison.Ordinal);

        Assert.True(openMenuIndex >= 0, "Expected admin layout to define openMenu(menu).");
        Assert.True(anchorIndex > openMenuIndex, "Expected openMenu(menu) to resolve an anchor.");
        Assert.True(guardIndex > anchorIndex, "Expected openMenu(menu) to guard against a missing/invalid anchor before continuing.");
        Assert.True(attachIndex > guardIndex, "Expected floatingLayer.attach(menu) to happen only after the anchor guard.");
    }

    [Fact]
    public void AdminLayout_OpenMenu_RepositionsAgainBeforeReveal()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.Matches(
            new Regex(
                @"menu\._menuRevealFrame\s*=\s*requestAnimationFrame\(\(\)\s*=>\s*\{[\s\S]*positionMenu\(menu\);[\s\S]*menu\.style\.visibility\s*=\s*'';",
                RegexOptions.Multiline),
            layout);
    }

    [Fact]
    public void SharedFloatingLayer_UsesImportantInlinePositioningForPortaledMenus()
    {
        var scriptPath = Path.Combine(RepoRoot, "HV-Travel.Web", "wwwroot", "js", "admin-floating-layer.js");
        var script = File.ReadAllText(scriptPath);

        Assert.Contains("element.style.setProperty('left'", script, StringComparison.Ordinal);
        Assert.Contains("element.style.setProperty('top'", script, StringComparison.Ordinal);
        Assert.Contains("element.style.setProperty('right', 'auto', 'important')", script, StringComparison.Ordinal);
        Assert.Contains("element.style.setProperty('bottom', 'auto', 'important')", script, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminLayout_PortalAttachedMenus_DoNotTransitionPositionProperties()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.Matches(
            new Regex(@"\.admin-menu-portal-attached\s*\{[\s\S]*transition-property:\s*opacity,\s*transform,\s*filter,\s*box-shadow\s*!important;", RegexOptions.Multiline),
            layout);
    }

    [Fact]
    public void AdminLayout_UsesNotificationDropdownViewComponent_InsteadOfHardcodedSampleItems()
    {
        var layoutPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", "Shared", "_Layout.cshtml");
        var layout = File.ReadAllText(layoutPath);

        Assert.Contains("@await Component.InvokeAsync(\"AdminNotificationDropdown\")", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Đơn hàng <span class=\"font-bold\">#BK-9420</span> đã thanh toán thành công", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Khách hàng mới <span class=\"font-bold\">Nguyễn Văn A</span> vừa đăng ký", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("Tour <span class=\"font-bold\">Hà Giang Loop</span> nhận được đánh giá 5 sao", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminNotificationDropdownComponent_RendersPassiveReadOnlyCopy_InsteadOfDeadActions()
    {
        var componentViewPath = Path.Combine(
            RepoRoot,
            "HV-Travel.Web",
            "Views",
            "Shared",
            "Components",
            "AdminNotificationDropdown",
            "Default.cshtml");

        var view = File.ReadAllText(componentViewPath);

        Assert.Contains("Chế độ chỉ đọc", view, StringComparison.Ordinal);
        Assert.Contains("Thông báo đồng bộ từ hệ thống.", view, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"#\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Đánh dấu đã đọc</button>", view, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Bookings", true)]
    [InlineData("Payments", false)]
    [InlineData("Customers", false)]
    [InlineData("Reviews", true)]
    [InlineData("Tours", false)]
    [InlineData("Users", false)]
    public void AdminListViews_DeclareExplicitMenuAnchorContract(string viewFolder, bool includesDateMenu)
    {
        var viewPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", viewFolder, "Index.cshtml");
        var view = File.ReadAllText(viewPath);

        Assert.Contains("id=\"filter-menu-trigger\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-target=\"filter-menu\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"filter-menu\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-haspopup=\"menu\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-expanded=\"false\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-anchor-id=\"filter-menu-trigger\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-align=\"right\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-placement=\"auto\"", view, StringComparison.Ordinal);

        Assert.Contains("id=\"column-menu-trigger\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-target=\"column-menu\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"column-menu\"", view, StringComparison.Ordinal);
        Assert.Contains("data-menu-anchor-id=\"column-menu-trigger\"", view, StringComparison.Ordinal);

        if (includesDateMenu)
        {
            Assert.Contains("id=\"date-dropdown-trigger\"", view, StringComparison.Ordinal);
            Assert.Contains("data-menu-target=\"date-dropdown\"", view, StringComparison.Ordinal);
            Assert.Contains("aria-controls=\"date-dropdown\"", view, StringComparison.Ordinal);
            Assert.Contains("data-menu-anchor-id=\"date-dropdown-trigger\"", view, StringComparison.Ordinal);
        }
    }

    [Theory]
    [InlineData("Bookings")]
    [InlineData("Payments")]
    [InlineData("Customers")]
    [InlineData("Reviews")]
    [InlineData("Tours")]
    [InlineData("Users")]
    public void AdminListViews_DoNotDependOnExactOnclickSelectorsForMenuOutsideClick(string viewFolder)
    {
        var viewPath = Path.Combine(RepoRoot, "HV-Travel.Web", "Areas", "Admin", "Views", viewFolder, "Index.cshtml");
        var view = File.ReadAllText(viewPath);

        Assert.DoesNotContain("event.target.closest('button[onclick=\"toggleFilterMenu()\"]')", view, StringComparison.Ordinal);
        Assert.DoesNotContain("event.target.closest('button[onclick=\"toggleColumnMenu()\"]')", view, StringComparison.Ordinal);
        Assert.DoesNotContain("event.target.closest('button[onclick*=\"toggleBookingFilter(\\'date-dropdown\\')\"]')", view, StringComparison.Ordinal);
        Assert.DoesNotContain("event.target.closest('button[onclick*=\"toggleReviewFilter(\\'date-dropdown\\')\"]')", view, StringComparison.Ordinal);
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
