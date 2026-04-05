using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class ContentAdminCatalog
{
    private static readonly IReadOnlyList<ContentSubtabOption> BookingSubtabs = new List<ContentSubtabOption>
    {
        new() { Key = "consultation", Label = "Tư vấn", Description = "Hero, quyền lợi, form và trạng thái gửi yêu cầu" },
        new() { Key = "create", Label = "Tạo booking", Description = "Hero, stepper, form hành khách và báo giá" },
        new() { Key = "payment", Label = "Thanh toán", Description = "Hero, stepper, phương thức thanh toán, đối soát và timeline" },
        new() { Key = "success", Label = "Thành công", Description = "Thông điệp đặt tour thành công" },
        new() { Key = "failed", Label = "Thất bại", Description = "Thông điệp thanh toán thất bại" },
        new() { Key = "error", Label = "Lỗi hệ thống", Description = "Thông điệp lỗi và hướng dẫn hỗ trợ" }
    };

    private static readonly IReadOnlyList<ContentTabOption> Tabs = new List<ContentTabOption>
    {
        new() { Key = "site", Label = "Toàn site", Description = "Header, footer, điều hướng, tiện ích tài khoản, liên hệ, SEO" },
        new() { Key = "home", Label = "Trang chủ", Description = "Hero, chỉ số, tour nổi bật, cam kết, CTA" },
        new() { Key = "about", Label = "Giới thiệu", Description = "Hero, câu chuyện, sứ mệnh, đội ngũ" },
        new() { Key = "contact", Label = "Liên hệ", Description = "Hero, thẻ liên hệ, mở đầu form" },
        new() { Key = "publicTours", Label = "Danh sách tour", Description = "Hero, chip bộ sưu tập, bộ lọc, kết quả và trạng thái rỗng" },
        new() { Key = "publicTourDetails", Label = "Chi tiết tour", Description = "Shell copy cho trang tour detail: badge, heading, CTA và helper copy" },
        new() { Key = "destinations", Label = "Điểm đến", Description = "Hero, mở đầu bộ sưu tập và mở đầu theo vùng" },
        new() { Key = "promotions", Label = "Khuyến mãi", Description = "Hero và các section flash sale, voucher, deal theo mùa" },
        new() { Key = "services", Label = "Dịch vụ", Description = "Hero, thẻ dịch vụ và giới thiệu form báo giá" },
        new() { Key = "inspiration", Label = "Cẩm nang", Description = "Hero, bài nổi bật và danh sách bài mới" },
        new() { Key = "inspirationDetails", Label = "Chi tiết cẩm nang", Description = "Shell copy cho trang bài viết: badge, heading, empty state và CTA" },
        new() { Key = "booking", Label = "Đặt tour", Description = "Tư vấn, tạo booking, thanh toán và các màn trạng thái booking" },
        new() { Key = "bookingLookup", Label = "Tra cứu booking", Description = "Hero, form tra cứu và trạng thái sẵn sàng" },
        new() { Key = "customerLogin", Label = "Đăng nhập khách hàng", Description = "Hero, feature cards, intro form và CTA sang đăng ký" },
        new() { Key = "customerRegister", Label = "Đăng ký khách hàng", Description = "Hero, quyền lợi, intro form và CTA sang đăng nhập" },
        new() { Key = "customerPortal", Label = "Customer portal", Description = "Hero, stats, booking, review, voucher, traveller và notifications" }
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
                   string.Equals(editor.TabKey, normalizedTab, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(editor.SubtabKey ?? string.Empty, normalizedSubtab ?? string.Empty, StringComparison.OrdinalIgnoreCase))
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
                Breadcrumb = new List<string> { "Nội dung website", "Toàn site" }
            },
            Editor(
                "home",
                "Trang chủ",
                "Trang chủ",
                "Quản lý nội dung hiển thị trên trang chủ theo từng section lớn của trang.",
                "6 section chính của trang chủ.",
                Section("hero", "Hero trang chủ", "Tiêu đề chính, mô tả và CTA đầu trang"),
                Section("carousel", "Carousel trang chủ", "5 slide ảnh marketing nằm ngay dưới hero"),
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
                "Quản lý hero, chip bộ sưu tập, nhãn bộ lọc, phần kết quả và trạng thái rỗng của trang danh sách tour.",
                "5 section dành cho trang danh sách tour.",
                Section("indexHero", "Hero danh sách tour", "Badge, tiêu đề, mô tả và cụm tìm kiếm"),
                Section("collectionChips", "Chip bộ sưu tập", "Nhãn các chip collection nằm trên danh sách tour"),
                Section("filterPanel", "Nhãn bộ lọc", "Toàn bộ label, option và CTA của cột bộ lọc"),
                Section("resultsPanel", "Nhãn phần kết quả", "Eyebrow, tiêu đề kết quả, wishlist và recently viewed"),
                Section("emptyState", "Trạng thái rỗng", "Thông điệp và CTA khi không có tour phù hợp")),
            Editor(
                "publicTourDetails",
                "Chi tiết tour",
                "Trang chi tiết tour",
                "Chỉ quản lý shell copy, CTA và fallback text trên trang tour detail; dữ liệu record-level vẫn lấy từ Tour.",
                "9 section shell-copy cho trang chi tiết tour.",
                Section("hero", "Hero chi tiết tour", "Badge xác nhận, badge hủy, badge seat, meta label và helper copy"),
                Section("highlights", "Điểm nổi bật", "Tiêu đề, badge phụ và empty state cho highlights"),
                Section("overview", "Tổng quan", "Eyebrow, heading và helper copy của phần tổng quan"),
                Section("inclusions", "Bao gồm / không bao gồm", "Heading và empty state cho inclusions, exclusions"),
                Section("schedule", "Lịch trình", "Heading và empty state cho schedule"),
                Section("policies", "Chính sách", "Heading, fallback copy cho cancellation và meeting point"),
                Section("departures", "Lịch khởi hành", "Heading, cột bảng và helper copy cho departures"),
                Section("bookingPanel", "Sidebar đặt tour", "Eyebrow, helper copy và CTA của khối booking sidebar"),
                Section("relatedTours", "Tour liên quan", "Heading và empty state cho related tours")),
            Editor(
                "destinations",
                "Điểm đến",
                "Trang điểm đến",
                "Quản lý hero, phần mở đầu bộ sưu tập nổi bật và phần mở đầu khám phá theo vùng.",
                "3 section của trang điểm đến.",
                Section("hero", "Hero điểm đến", "Badge, tiêu đề và mô tả mở đầu của hub điểm đến"),
                Section("collectionsIntro", "Mở đầu bộ sưu tập", "Tiêu đề và mô tả cho khu vực bộ sưu tập nổi bật"),
                Section("regionsIntro", "Mở đầu theo vùng", "Tiêu đề và mô tả cho khu vực khám phá theo vùng")),
            Editor(
                "promotions",
                "Khuyến mãi",
                "Trang khuyến mãi",
                "Quản lý hero landing, intro flash sale, ví voucher và deal theo mùa.",
                "4 section của trang khuyến mãi.",
                Section("hero", "Hero khuyến mãi", "Badge, tiêu đề và mô tả hero của landing ưu đãi"),
                Section("flashSalesIntro", "Mở đầu flash sale", "Tiêu đề và mô tả cho khối flash sale"),
                Section("voucherIntro", "Mở đầu ví voucher", "Tiêu đề và mô tả cho khối voucher campaign"),
                Section("seasonalDealsIntro", "Mở đầu deal theo mùa", "Tiêu đề và mô tả cho khối deal theo mùa")),
            Editor(
                "services",
                "Dịch vụ",
                "Trang dịch vụ lẻ",
                "Quản lý hero, copy cho 4 thẻ dịch vụ và phần mở đầu form yêu cầu báo giá.",
                "3 section của trang dịch vụ lẻ.",
                Section("hero", "Hero dịch vụ", "Badge, tiêu đề và mô tả mở đầu của trang dịch vụ"),
                Section("serviceCards", "Nội dung thẻ dịch vụ", "Tiêu đề và mô tả cho 4 thẻ Vé máy bay, Khách sạn, Combo và Visa"),
                Section("quoteFormIntro", "Giới thiệu form báo giá", "Tiêu đề, mô tả và nút gửi của form báo giá")),
            Editor(
                "inspiration",
                "Cẩm nang",
                "Hub cẩm nang",
                "Quản lý hero hub nội dung, phần mở đầu bài nổi bật và danh sách bài mới.",
                "3 section của hub cẩm nang.",
                Section("hero", "Hero cẩm nang", "Badge, tiêu đề và mô tả mở đầu của hub cẩm nang"),
                Section("featuredIntro", "Mở đầu bài nổi bật", "Tiêu đề và mô tả cho khu vực bài viết nổi bật"),
                Section("latestIntro", "Mở đầu bài mới", "Tiêu đề và mô tả cho danh sách bài viết mới")),
            Editor(
                "inspirationDetails",
                "Chi tiết cẩm nang",
                "Trang chi tiết bài viết",
                "Chỉ quản lý shell copy của hero, phần thân bài và tags; nội dung bài vẫn lấy từ TravelArticle.",
                "3 section shell-copy cho trang chi tiết bài viết.",
                Section("hero", "Hero bài viết", "Eyebrow, heading phụ và meta copy cho hero bài viết"),
                Section("body", "Thân bài", "Heading, mô tả phụ và empty state cho thân bài"),
                Section("tags", "Tags", "Heading và empty state cho danh sách tags")),
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
                SubtabKey = "create",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Tạo booking",
                Description = "Quản lý shell copy cho bước xác nhận hành khách và báo giá trước khi thanh toán.",
                ScopeSummary = "4 section cho màn tạo booking.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Tạo booking" },
                Subtabs = BookingSubtabs,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("createHero", "Hero tạo booking", "Badge, tiêu đề và mô tả đầu trang"),
                    Section("createStepper", "Stepper tạo booking", "Nhãn các bước trong flow booking"),
                    Section("travellerForm", "Form hành khách", "Nhãn section, label form, placeholder và empty state lịch khởi hành"),
                    Section("pricingPanel", "Panel báo giá", "Heading, nhãn tiền tệ, CTA và helper copy của báo giá")
                }
            },
            new()
            {
                TabKey = "booking",
                SubtabKey = "payment",
                PageKey = "booking",
                NavigationLabel = "Đặt tour",
                Title = "Đặt tour / Thanh toán",
                Description = "Quản lý shell copy cho bước thanh toán, đối soát và timeline booking.",
                ScopeSummary = "6 section cho màn thanh toán.",
                Breadcrumb = new List<string> { "Nội dung website", "Đặt tour", "Thanh toán" },
                Subtabs = BookingSubtabs,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("paymentHero", "Hero thanh toán", "Badge, tiêu đề và mô tả đầu trang"),
                    Section("paymentStepper", "Stepper thanh toán", "Nhãn các bước trong flow booking"),
                    Section("paymentMethods", "Phương thức thanh toán", "Heading, label option và CTA xác nhận thanh toán"),
                    Section("transferProof", "Minh chứng chuyển khoản", "Heading, placeholder note và CTA tải minh chứng"),
                    Section("orderSummary", "Tóm tắt đơn", "Heading, label số liệu và helper copy của panel booking"),
                    Section("paymentTimeline", "Timeline thanh toán", "Heading và empty state cho timeline xử lý")
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
                Subtabs = BookingSubtabs,
                DependencyNotes = ContactInfoDependency,
                Sections = new List<ContentAdminSectionDefinition>
                {
                    Section("statusCopy", "Thông điệp lỗi hệ thống", "Chỉ chỉnh copy cho màn lỗi hệ thống", false, "Field slice", "errorTitle", "errorDescription")
                }
            },
            Editor(
                "bookingLookup",
                "Tra cứu booking",
                "Trang tra cứu booking",
                "Quản lý hero, phần giới thiệu form tra cứu và trạng thái sẵn sàng trước khi có kết quả.",
                "3 section của trang tra cứu booking.",
                Section("hero", "Hero tra cứu booking", "Badge, tiêu đề và mô tả phần mở đầu"),
                Section("lookupForm", "Giới thiệu form tra cứu", "Tiêu đề, mô tả và nút submit của form tra cứu"),
                Section("readyState", "Trạng thái sẵn sàng", "Thông điệp hiển thị trước khi người dùng tra cứu booking")),
            Editor(
                "customerLogin",
                "Đăng nhập khách hàng",
                "Trang đăng nhập khách hàng",
                "Quản lý hero, feature cards, intro form và CTA sang đăng ký.",
                "4 section của trang đăng nhập khách hàng.",
                Section("hero", "Hero đăng nhập", "Badge, tiêu đề và mô tả mở đầu"),
                Section("featureCards", "Feature cards", "Tiêu đề và mô tả các điểm nhấn của tài khoản khách hàng"),
                Section("formIntro", "Giới thiệu form", "Eyebrow, tiêu đề, mô tả và helper copy của form đăng nhập"),
                Section("registerPrompt", "CTA đăng ký", "Thông điệp và nút chuyển sang đăng ký")),
            Editor(
                "customerRegister",
                "Đăng ký khách hàng",
                "Trang đăng ký khách hàng",
                "Quản lý hero, benefits, intro form và CTA sang đăng nhập.",
                "4 section của trang đăng ký khách hàng.",
                Section("hero", "Hero đăng ký", "Badge, tiêu đề và mô tả mở đầu"),
                Section("benefits", "Quyền lợi", "Heading, title và mô tả cho các thẻ quyền lợi"),
                Section("formIntro", "Giới thiệu form", "Eyebrow, tiêu đề, mô tả và helper copy của form đăng ký"),
                Section("loginPrompt", "CTA đăng nhập", "Thông điệp và nút chuyển sang đăng nhập")),
            Editor(
                "customerPortal",
                "Customer portal",
                "Trang customer portal",
                "Chỉ quản lý shell copy cho hero, stats, booking, review, voucher, traveller và notification; dữ liệu portal vẫn giữ động.",
                "7 section shell-copy của customer portal.",
                Section("hero", "Hero portal", "Eyebrow, tiêu đề, mô tả và khối tier summary"),
                Section("stats", "Thống kê portal", "Label 4 khối số liệu đầu trang"),
                Section("bookingPanel", "Khối booking", "Heading, helper copy, CTA và empty state cho booking history"),
                Section("reviewPanel", "Khối review", "Heading, empty state và CTA của review request"),
                Section("voucherPanel", "Khối voucher", "Heading, empty state và helper copy của voucher wallet"),
                Section("travellerPanel", "Khối traveller", "Heading, placeholder form và CTA lưu traveller"),
                Section("notificationsPanel", "Khối notifications", "Heading và empty state cho notifications"))
        };
    }

    private static ContentAdminEditorDefinition Editor(
        string tabKey,
        string navigationLabel,
        string title,
        string description,
        string scopeSummary,
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
