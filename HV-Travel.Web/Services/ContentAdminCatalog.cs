using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class ContentAdminCatalog
{
    private static readonly IReadOnlyList<ContentSubtabOption> BookingSubtabs = new List<ContentSubtabOption>
    {
        new() { Key = "consultation", Label = "Tư vấn", Description = "Hero, quyền lợi, form và trạng thái gửi yêu cầu" },
        new() { Key = "success", Label = "Thành công", Description = "Thông điệp đặt tour thành công" },
        new() { Key = "failed", Label = "Thất bại", Description = "Thông điệp thanh toán thất bại" },
        new() { Key = "error", Label = "Lỗi hệ thống", Description = "Thông điệp lỗi và hướng dẫn hỗ trợ" }
    };

    private static readonly IReadOnlyList<ContentTabOption> Tabs = new List<ContentTabOption>
    {
        new() { Key = "site", Label = "Toàn site", Description = "Header, footer, thương hiệu, liên hệ, SEO" },
        new() { Key = "home", Label = "Trang chủ", Description = "Hero, chỉ số, tour nổi bật, cam kết, CTA" },
        new() { Key = "about", Label = "Giới thiệu", Description = "Hero, câu chuyện, sứ mệnh, đội ngũ" },
        new() { Key = "contact", Label = "Liên hệ", Description = "Hero, thẻ liên hệ, mở đầu form" },
        new() { Key = "publicTours", Label = "Danh sách tour", Description = "Hero danh sách và trạng thái rỗng" },
        new() { Key = "booking", Label = "Đặt tour", Description = "Tư vấn và các màn trạng thái booking" }
    };

    private static readonly IReadOnlyList<ContentDependencyNoteViewModel> ContactInfoDependency = new List<ContentDependencyNoteViewModel>
    {
        new()
        {
            Title = "Dùng dữ liệu từ Toàn site > contactInfo",
            Description = "Địa chỉ, số điện thoại, email, giờ làm việc và URL bản đồ đang được dùng chung. Muốn đổi các thông tin đó, hãy chỉnh trong phần Toàn site.",
            NavigateTab = "site"
        }
    };

    private static readonly IReadOnlyList<ContentAdminEditorDefinition> Editors = BuildEditors();

    public static IReadOnlyList<ContentTabOption> GetTabs() => Tabs;

    public static IReadOnlyList<ContentSubtabOption> GetSubtabs(string tabKey)
    {
        return string.Equals(tabKey, "booking", StringComparison.OrdinalIgnoreCase)
            ? BookingSubtabs
            : Array.Empty<ContentSubtabOption>();
    }

    public static ContentAdminEditorDefinition Resolve(string? tab, string? subtab)
    {
        var normalizedTab = Tabs.Any(item => string.Equals(item.Key, tab, StringComparison.OrdinalIgnoreCase))
            ? tab!.Trim()
            : "site";

        var normalizedSubtab = string.Equals(normalizedTab, "booking", StringComparison.OrdinalIgnoreCase)
            ? NormalizeBookingSubtab(subtab)
            : null;

        return Editors.FirstOrDefault(editor =>
                   string.Equals(editor.TabKey, normalizedTab, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(editor.SubtabKey ?? string.Empty, normalizedSubtab ?? string.Empty, StringComparison.OrdinalIgnoreCase))
               ?? Editors.First(editor => editor.TabKey == "site");
    }

    private static string NormalizeBookingSubtab(string? subtab)
    {
        return BookingSubtabs.Any(item => string.Equals(item.Key, subtab, StringComparison.OrdinalIgnoreCase))
            ? subtab!.Trim()
            : "consultation";
    }

    private static IReadOnlyList<ContentAdminEditorDefinition> BuildEditors()
    {
        return new List<ContentAdminEditorDefinition>
        {
            new()
            {
                TabKey = "site",
                PageKey = "site",
                NavigationLabel = "Toàn site",
                Title = "Cấu hình toàn site",
                Description = "Quản lý các nhóm nội dung dùng chung như header, footer, thông tin liên hệ và SEO mặc định.",
                ScopeSummary = "7 nhóm site-wide đang dùng trên toàn bộ public site.",
                Breadcrumb = new List<string> { "Nội dung website", "Toàn site" },
                PreviewTarget = new ContentPreviewTarget { Controller = "Home", Action = "Index" }
            },
            Editor(
                "home",
                "Trang chủ",
                "Trang chủ",
                "Quản lý nội dung hiển thị trên trang chủ theo từng section lớn của trang.",
                "5 section chính của trang chủ.",
                new ContentPreviewTarget { Controller = "Home", Action = "Index" },
                Section("hero", "Hero trang chủ", "Tiêu đề chính, mô tả và CTA đầu trang"),
                Section("stats", "Chỉ số marketing", "Các số liệu nổi bật bên dưới hero"),
                Section("featuredToursIntro", "Giới thiệu tour nổi bật", "Tiêu đề và mô tả khối tour nổi bật"),
                Section("commitments", "Cam kết chất lượng", "Các điểm mạnh và thông điệp cam kết"),
                Section("finalCta", "CTA cuối trang", "Khối kêu gọi hành động ở cuối trang")),
            Editor(
                "about",
                "Giới thiệu",
                "Trang giới thiệu",
                "Quản lý các khối nội dung kể câu chuyện thương hiệu, sứ mệnh và đội ngũ.",
                "4 section chính của trang giới thiệu.",
                new ContentPreviewTarget { Controller = "Home", Action = "About" },
                Section("hero", "Hero giới thiệu", "Tiêu đề mở đầu và mô tả trang giới thiệu"),
                Section("story", "Câu chuyện thương hiệu", "Nội dung phần hành trình hình thành"),
                Section("missionVision", "Sứ mệnh và tầm nhìn", "Thông điệp định hướng của thương hiệu"),
                Section("team", "Đội ngũ", "Thông tin thành viên và vai trò")),
            new()
            {
                TabKey = "contact",
                PageKey = "contact",
                NavigationLabel = "Liên hệ",
                Title = "Trang liên hệ",
                Description = "Quản lý hero, tiêu đề thẻ liên hệ và phần giới thiệu form trên trang liên hệ.",
                ScopeSummary = "3 section của trang liên hệ và 1 phụ thuộc site-wide.",
                Breadcrumb = new List<string> { "Nội dung website", "Liên hệ" },
                PreviewTarget = new ContentPreviewTarget { Controller = "Home", Action = "Contact" },
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("hero", "Hero liên hệ", "Tiêu đề và mô tả phần mở đầu"),
                    Section("cards", "Nhãn thẻ liên hệ", "Tiêu đề các thẻ địa chỉ, điện thoại, email, giờ làm việc"),
                    Section("formIntro", "Giới thiệu form liên hệ", "Tiêu đề, mô tả và nút gửi của form")
                }
            },
            Editor(
                "publicTours",
                "Danh sách tour",
                "Trang danh sách tour",
                "Quản lý phần mở đầu của trang danh sách tour và trạng thái rỗng khi không có kết quả.",
                "2 section dành cho trang danh sách tour.",
                new ContentPreviewTarget { Controller = "PublicTours", Action = "Index" },
                Section("indexHero", "Hero danh sách tour", "Tiêu đề, mô tả và placeholder tìm kiếm"),
                Section("emptyState", "Trạng thái rỗng", "Thông điệp và CTA khi không có tour phù hợp")),
            new()
            {
                TabKey = "booking",
                SubtabKey = "consultation",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Tư vấn",
                Description = "Quản lý nội dung cho màn tư vấn chuyến đi và form gửi yêu cầu.",
                ScopeSummary = "2 section của màn tư vấn và 1 phụ thuộc site-wide.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Tư vấn" },
                PreviewTarget = new ContentPreviewTarget { Controller = "Booking", Action = "Consultation" },
                Subtabs = BookingSubtabs,
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("consultationHero", "Hero tư vấn", "Tiêu đề và mô tả đầu trang"),
                    Section("consultationBenefits", "Quyền lợi và form tư vấn", "Thông điệp liên hệ nhanh, lý do chọn, nội dung submit và trạng thái gửi yêu cầu")
                }
            },
            new()
            {
                TabKey = "booking",
                SubtabKey = "success",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Thành công",
                Description = "Chỉnh thông điệp hiển thị khi người dùng hoàn tất đặt tour thành công.",
                ScopeSummary = "1 khối copy dùng chung cho màn thành công.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Thành công" },
                PreviewUnavailableReason = "Màn này cần một booking thực tế để preview trực tiếp.",
                Subtabs = BookingSubtabs,
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("statusCopy", "Thông điệp thành công", "Chỉ chỉnh copy cho trạng thái thành công", false, "Field slice", "successTitle", "successDescription")
                }
            },
            new()
            {
                TabKey = "booking",
                SubtabKey = "failed",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Thất bại",
                Description = "Chỉnh thông điệp cho màn thanh toán thất bại mà không ảnh hưởng trạng thái khác.",
                ScopeSummary = "1 khối copy dùng chung cho màn thất bại.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Thất bại" },
                PreviewUnavailableReason = "Màn này cần một booking thực tế để preview trực tiếp.",
                Subtabs = BookingSubtabs,
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("statusCopy", "Thông điệp thất bại", "Chỉ chỉnh copy cho trạng thái thanh toán thất bại", false, "Field slice", "failedTitle", "failedDescription")
                }
            },
            new()
            {
                TabKey = "booking",
                SubtabKey = "error",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Lỗi hệ thống",
                Description = "Chỉnh thông điệp xuất hiện khi có lỗi hệ thống trong luồng đặt tour.",
                ScopeSummary = "1 khối copy dùng chung cho màn lỗi hệ thống.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Lỗi hệ thống" },
                PreviewTarget = new ContentPreviewTarget { Controller = "Booking", Action = "Error" },
                Subtabs = BookingSubtabs,
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("statusCopy", "Thông điệp lỗi hệ thống", "Chỉ chỉnh copy cho màn lỗi hệ thống", false, "Field slice", "errorTitle", "errorDescription")
                }
            }
        };
    }

    private static ContentAdminEditorDefinition Editor(
        string tabKey,
        string navigationLabel,
        string title,
        string description,
        string scopeSummary,
        ContentPreviewTarget previewTarget,
        params ContentAdminSectionDefinition[] sections)
    {
        return new ContentAdminEditorDefinition
        {
            TabKey = tabKey,
            PageKey = tabKey,
            NavigationLabel = navigationLabel,
            Title = title,
            Description = description,
            ScopeSummary = scopeSummary,
            Breadcrumb = new List<string> { "Nội dung website", navigationLabel },
            PreviewTarget = previewTarget,
            Sections = sections
        };
    }

    private static ContentAdminSectionDefinition Section(
        string sectionKey,
        string cardTitle,
        string cardDescription,
        bool allowSectionSettings = true,
        string scopeLabel = "Toàn bộ section",
        params string[] editableFieldKeys)
    {
        return new ContentAdminSectionDefinition
        {
            SectionKey = sectionKey,
            CardTitle = cardTitle,
            CardDescription = cardDescription,
            ScopeLabel = scopeLabel,
            AllowSectionSettings = allowSectionSettings,
            EditableFieldKeys = editableFieldKeys
        };
    }
}
