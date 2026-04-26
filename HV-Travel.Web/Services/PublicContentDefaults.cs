using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class PublicContentDefaults
{
    public static IReadOnlyList<ContentTabOption> Tabs => new List<ContentTabOption>
    {
        new() { Key = "site", Label = "Toàn site", Description = "Header, footer, thương hiệu, mạng xã hội, liên hệ" },
        new() { Key = "home", Label = "Trang chủ", Description = "Hero, chỉ số, tour nổi bật, cam kết, CTA" },
        new() { Key = "about", Label = "Giới thiệu", Description = "Hero, câu chuyện, sứ mệnh/tầm nhìn, đội ngũ" },
        new() { Key = "contact", Label = "Liên hệ", Description = "Hero, thẻ liên hệ, giới thiệu biểu mẫu" },
        new() { Key = "publicTours", Label = "Tour", Description = "Hero, ô tìm kiếm, trạng thái rỗng" },
        new() { Key = "publicTourDetails", Label = "Chi tiết tour", Description = "Hero, heading, CTA và helper copy cho tour detail" },
        new() { Key = "destinations", Label = "Điểm đến", Description = "Hero, bộ sưu tập nổi bật, khám phá theo vùng" },
        new() { Key = "promotions", Label = "Khuyến mãi", Description = "Hero, flash sale, voucher, deal theo mùa" },
        new() { Key = "services", Label = "Dịch vụ", Description = "Hero, thẻ dịch vụ, form báo giá" },
        new() { Key = "inspiration", Label = "Cẩm nang", Description = "Hero, bài nổi bật, danh sách bài mới" },
        new() { Key = "inspirationDetails", Label = "Chi tiết cẩm nang", Description = "Hero, heading, tags và empty state của bài viết" },
        new() { Key = "booking", Label = "Đặt tour", Description = "Hero tư vấn, tạo booking, thanh toán và nội dung trạng thái" },
        new() { Key = "bookingLookup", Label = "Tra cứu booking", Description = "Hero, form tra cứu, trạng thái sẵn sàng" },
        new() { Key = "customerLogin", Label = "Đăng nhập khách hàng", Description = "Hero, feature cards, form intro và CTA đăng ký" },
        new() { Key = "customerRegister", Label = "Đăng ký khách hàng", Description = "Hero, benefits, form intro và CTA đăng nhập" },
        new() { Key = "customerPortal", Label = "Cổng khách hàng", Description = "Hero, thống kê, booking, đánh giá, voucher, hành khách và thông báo" }
    };

    public static Dictionary<string, List<string>> Inventory => new()
    {
        ["site"] = new() { "header", "footerBrand", "footerExplore", "footerCompany", "contactInfo", "socialLinks", "seo" },
        ["home"] = new() { "hero", "carousel", "stats", "featuredToursIntro", "commitments", "finalCta" },
        ["about"] = new() { "hero", "story", "missionVision", "team" },
        ["contact"] = new() { "hero", "cards", "formIntro" },
        ["publicTours"] = new() { "indexHero", "collectionChips", "filterPanel", "resultsPanel", "emptyState" },
        ["publicTourDetails"] = new() { "hero", "highlights", "overview", "inclusions", "schedule", "routing", "policies", "departures", "bookingPanel", "relatedTours" },
        ["destinations"] = new() { "hero", "collectionsIntro", "regionsIntro" },
        ["promotions"] = new() { "hero", "flashSalesIntro", "voucherIntro", "seasonalDealsIntro" },
        ["services"] = new() { "hero", "serviceCards", "quoteFormIntro" },
        ["inspiration"] = new() { "hero", "featuredIntro", "latestIntro" },
        ["inspirationDetails"] = new() { "hero", "body", "tags" },
        ["booking"] = new() { "consultationHero", "consultationBenefits", "statusCopy", "createHero", "createStepper", "travellerForm", "pricingPanel", "paymentHero", "paymentStepper", "paymentMethods", "transferProof", "orderSummary", "paymentTimeline" },
        ["bookingLookup"] = new() { "hero", "lookupForm", "readyState" },
        ["customerLogin"] = new() { "hero", "featureCards", "formIntro", "registerPrompt" },
        ["customerRegister"] = new() { "hero", "benefits", "formIntro", "loginPrompt" },
        ["customerPortal"] = new() { "hero", "stats", "bookingPanel", "reviewPanel", "voucherPanel", "travellerPanel", "notificationsPanel" }
    };

    public static SiteSettings CreateSiteSettings()
    {
        return new SiteSettings
        {
            SettingsKey = "default",
            Groups = new List<SiteSettingsGroup>
            {
                Group("header", "Header", 1, new List<ContentField>
                {
                    Text("brandName", "Tên thương hiệu", "HV Travel"),
                    Text("brandTagline", "Khẩu hiệu", "Khám phá Việt Nam"),
                    Text("navHomeLabel", "Nhãn trang chủ", "Trang chủ"),
                    Text("navToursLabel", "Nhãn tour", "Tour du lịch"),
                    Text("navDestinationsLabel", "Nhãn điểm đến", "Điểm đến"),
                    Text("navPromotionsLabel", "Nhãn khuyến mãi", "Ưu đãi"),
                    Text("navInspirationLabel", "Nhãn cẩm nang", "Cẩm nang"),
                    Text("navServicesLabel", "Nhãn dịch vụ", "Dịch vụ"),
                    Text("navAboutLabel", "Nhãn giới thiệu", "Giới thiệu"),
                    Text("navContactLabel", "Nhãn liên hệ", "Liên hệ"),
                    Text("bookingLookupLabel", "Nhãn tra cứu booking", "Tra cứu booking"),
                    Text("moreLabel", "Nhãn khám phá thêm", "Khám phá thêm"),
                    Text("openPortalLabel", "Nhãn mở portal", "Mở portal"),
                    Text("customerFallbackLabel", "Tên khách hàng mặc định", "Khách hàng"),
                    Text("customerCodeFallbackLabel", "Mã khách hàng mặc định", "Khách hàng HV Travel"),
                    Text("logoutLabel", "Nhãn đăng xuất", "Đăng xuất"),
                    Text("registerLabel", "Nút đăng ký", "Đăng ký"),
                    Text("loginLabel", "Nút đăng nhập", "Đăng nhập")
                }),
                Group("footerBrand", "Thương hiệu footer", 2, new List<ContentField>
                {
                    Text("title", "Tiêu đề", "HV Travel"),
                    Text("tagline", "Khẩu hiệu", "Khám phá Việt Nam"),
                    TextArea("description", "Mô tả footer", "Khám phá vẻ đẹp Việt Nam với những tour du lịch được thiết kế riêng. Trải nghiệm văn hóa, ẩm thực và thiên nhiên tuyệt vời."),
                    Text("allRightsReservedLabel", "Nhãn bảo lưu bản quyền", "Bảo lưu mọi quyền.")
                }),
                Group("footerExplore", "Footer khám phá", 3, new List<ContentField>
                {
                    Text("title", "Tiêu đề khối", "Khám phá"),
                    Text("allToursLabel", "Link tất cả tour", "Tất cả tour"),
                    Text("popularToursLabel", "Link tour phổ biến", "Tour phổ biến"),
                    Text("hotDestinationsLabel", "Link điểm đến hot", "Điểm đến hot"),
                    Text("specialOffersLabel", "Link ưu đãi", "Ưu đãi đặc biệt")
                }),
                Group("footerCompany", "Footer công ty", 4, new List<ContentField>
                {
                    Text("title", "Tiêu đề khối", "Công ty"),
                    Text("aboutLabel", "Link giới thiệu", "Giới thiệu"),
                    Text("contactLabel", "Link liên hệ", "Liên hệ"),
                    Text("privacyLabel", "Link chính sách", "Chính sách bảo mật"),
                    Text("termsLabel", "Link điều khoản", "Điều khoản sử dụng"),
                    Text("sitemapLabel", "Link sơ đồ trang", "Sơ đồ trang")
                }),
                Group("contactInfo", "Thông tin liên hệ", 5, new List<ContentField>
                {
                    Text("title", "Tiêu đề", "Liên hệ"),
                    Text("address", "Địa chỉ", "123 Đường Nguyễn Huệ, Quận 1, TP.HCM"),
                    Text("phoneNumber", "Số điện thoại", "+84 (28) 3822 9999"),
                    Url("email", "Email", "info@hvtravel.vn"),
                    Text("businessHours", "Giờ làm việc", "T2 - T7: 8:00 - 18:00"),
                    Url("mapEmbedUrl", "Google Maps embed URL", "https://www.google.com/maps/embed?pb=!1m18!1m12!1m3!1d3919.5177!2d106.7!3d10.7769!2m3!1f0!2f0!3f0!3m2!1i1024!2i768!4f13.1!3m3!1m2!1s0x0%3A0x0!2zMTDCsDQ2JzM2LjkiTiAxMDbCsDQyJzAwLjAiRQ!5e0!3m2!1svi!2svn!4v1")
                }),
                Group("socialLinks", "Mạng xã hội", 6, new List<ContentField>
                {
                    Url("facebookUrl", "Facebook URL", "#"),
                    Url("instagramUrl", "Instagram URL", "#"),
                    Url("youtubeUrl", "YouTube URL", "#")
                }),
                Group("seo", "SEO mặc định", 7, new List<ContentField>
                {
                    TextArea("defaultMetaDescription", "Meta description mặc định", "HV Travel - Khám phá Việt Nam cùng những tour du lịch tuyệt vời nhất")
                })
            }
        };
    }

    public static List<ContentSection> CreateAllSections()
    {
        return new List<ContentSection>
        {
            Section("home", "hero", "Hero trang chủ", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Khám phá hơn 0+ điểm đến tuyệt vời"),
                Text("titleLine1", "Tiêu đề dòng 1", "Khám phá"),
                Text("titleHighlight", "Tiêu đề nhấn", "Việt Nam"),
                Text("titleLine2", "Tiêu đề dòng 2", "Theo cách của bạn"),
                TextArea("description", "Mô tả", "Những hành trình được thiết kế riêng, trải nghiệm văn hóa độc đáo và phong cảnh thiên nhiên tuyệt đẹp đang chờ đón bạn."),
                Text("primaryCtaText", "CTA chính", "Khám phá ngay"),
                Text("secondaryCtaText", "CTA phụ", "Liên hệ tư vấn")
            }),
            Section("home", "carousel", "Carousel trang chủ", 2, new List<ContentField>
            {
                Text("slide1SourceType", "Nguồn ảnh slide 1", "external"),
                Url("slide1ImageUrl", "Ảnh slide 1", "https://picsum.photos/id/1015/1600/900"),
                Url("slide1LinkUrl", "Link slide 1", "/PublicTours"),
                Text("slide2SourceType", "Nguồn ảnh slide 2", "external"),
                Url("slide2ImageUrl", "Ảnh slide 2", "https://picsum.photos/id/1016/1600/900"),
                Url("slide2LinkUrl", "Link slide 2", "/Destinations"),
                Text("slide3SourceType", "Nguồn ảnh slide 3", "external"),
                Url("slide3ImageUrl", "Ảnh slide 3", "https://picsum.photos/id/1018/1600/900"),
                Url("slide3LinkUrl", "Link slide 3", "/Promotions"),
                Text("slide4SourceType", "Nguồn ảnh slide 4", "external"),
                Url("slide4ImageUrl", "Ảnh slide 4", "https://picsum.photos/id/1020/1600/900"),
                Url("slide4LinkUrl", "Link slide 4", "/Services"),
                Text("slide5SourceType", "Nguồn ảnh slide 5", "external"),
                Url("slide5ImageUrl", "Ảnh slide 5", "https://picsum.photos/id/1039/1600/900"),
                Url("slide5LinkUrl", "Link slide 5", "/Inspiration")
            }),
            Section("home", "stats", "Chỉ số marketing trang chủ", 3, new List<ContentField>
            {
                Text("stat1Number", "Số liệu 1", "500+"),
                Text("stat1Label", "Nhãn 1", "Tour đã tổ chức"),
                Text("stat2Number", "Số liệu 2", "10K+"),
                Text("stat2Label", "Nhãn 2", "Khách hài lòng"),
                Text("stat3Number", "Số liệu 3", "50+"),
                Text("stat3Label", "Nhãn 3", "Điểm đến"),
                Text("stat4Number", "Số liệu 4", "4.9*"),
                Text("stat4Label", "Nhãn 4", "Đánh giá TB")
            }),
            Section("home", "featuredToursIntro", "Giới thiệu tour nổi bật trang chủ", 4, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tour nổi bật"),
                Text("title", "Tiêu đề", "Hành trình được yêu thích nhất"),
                TextArea("description", "Mô tả", "Những tour du lịch hàng đầu được đánh giá cao bởi hàng ngàn du khách"),
                Text("viewAllText", "Nút xem tất cả", "Xem tất cả tour"),
                Text("emptyStateText", "Thông báo khi chưa có tour", "Chưa có tour nào. Vui lòng quay lại sau.")
            }),
            Section("home", "commitments", "Cam kết trang chủ", 5, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tại sao chọn chúng tôi"),
                Text("title", "Tiêu đề", "Cam kết chất lượng"),
                TextArea("description", "Mô tả", "Dịch vụ chuyên nghiệp, lịch trình chỉn chu và trải nghiệm đáng nhớ trong từng hành trình"),
                Text("item1Title", "Cam kết 1", "An toàn tuyệt đối"),
                TextArea("item1Description", "Mô tả 1", "Lịch trình được tổ chức kỹ lưỡng với đội ngũ hỗ trợ giàu kinh nghiệm."),
                Text("item2Title", "Cam kết 2", "Trải nghiệm cao cấp"),
                TextArea("item2Description", "Mô tả 2", "Tour được thiết kế tinh gọn nhưng vẫn giữ chất lượng dịch vụ cao."),
                Text("item3Title", "Cam kết 3", "Hỗ trợ 24/7"),
                TextArea("item3Description", "Mô tả 3", "Đội ngũ của chúng tôi luôn sẵn sàng hỗ trợ trong suốt hành trình."),
                Text("item4Title", "Cam kết 4", "Giá tốt nhất"),
                TextArea("item4Description", "Mô tả 4", "Mức giá minh bạch, tối ưu ngân sách và phù hợp nhiều nhóm khách.")
            }),
            Section("home", "finalCta", "CTA cuối trang chủ", 6, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Sẵn sàng cho hành trình tiếp theo?"),
                TextArea("description", "Mô tả", "Hãy để HV Travel đồng hành cùng bạn trong chuyến đi tiếp theo với lịch trình được thiết kế riêng."),
                Text("primaryCtaText", "CTA chính", "Liên hệ ngay"),
                Text("secondaryCtaText", "CTA phụ", "Khám phá tour")
            }),
            Section("about", "hero", "Hero giới thiệu", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Về chúng tôi"),
                Text("titleLine1", "Tiêu đề dòng 1", "Câu chuyện"),
                Text("titleHighlight", "Tiêu đề nhấn", "HV Travel"),
                TextArea("description", "Mô tả", "Tìm hiểu hành trình phát triển và triết lý tạo nên những chuyến đi mang dấu ấn riêng của HV Travel.")
            }),
            Section("about", "story", "Câu chuyện giới thiệu", 2, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Hơn 10 năm kinh nghiệm trong ngành du lịch"),
                TextArea("description1", "Đoạn 1", "HV Travel được thành lập với niềm đam mê mang đến những trải nghiệm du lịch tuyệt vời nhất cho du khách."),
                TextArea("description2", "Đoạn 2", "Chúng tôi tin rằng mỗi chuyến đi không chỉ là một hành trình đến nơi mới, mà còn là cơ hội để khám phá bản thân, kết nối với văn hóa và con người.")
            }),
            Section("about", "missionVision", "Sứ mệnh và tầm nhìn", 3, new List<ContentField>
            {
                Text("title", "Tiêu đề section", "Sứ mệnh & Tầm nhìn"),
                Text("missionTitle", "Tiêu đề sứ mệnh", "Sứ mệnh"),
                TextArea("missionText", "Nội dung sứ mệnh", "Mang đến những hành trình được cá nhân hóa, an toàn và giàu cảm xúc cho mỗi du khách."),
                Text("visionTitle", "Tiêu đề tầm nhìn", "Tầm nhìn"),
                TextArea("visionText", "Nội dung tầm nhìn", "Trở thành thương hiệu du lịch được tin yêu hàng đầu cho những chuyến đi khám phá Việt Nam."),
                Text("valuesTitle", "Tiêu đề giá trị", "Giá trị cốt lõi"),
                TextArea("valuesText", "Nội dung giá trị", "Tận tâm, minh bạch, sáng tạo và luôn lấy trải nghiệm khách hàng làm trung tâm.")
            }),
            Section("about", "team", "Đội ngũ giới thiệu", 4, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Những con người đứng sau HV Travel"),
                TextArea("description", "Mô tả", "Đội ngũ tạo nên các hành trình chỉn chu và giàu cảm hứng cho từng chuyến đi."),
                Text("member1Name", "Tên thành viên 1", "Nguyễn Hoàng Vũ"),
                Text("member1Role", "Vai trò 1", "Nhà sáng lập"),
                Text("member2Name", "Tên thành viên 2", "Lê Minh Đức"),
                Text("member2Role", "Vai trò 2", "Trưởng phòng tour"),
                Text("member3Name", "Tên thành viên 3", "Trần Khánh Linh"),
                Text("member3Role", "Vai trò 3", "Chăm sóc khách hàng"),
                Text("member4Name", "Tên thành viên 4", "Phạm Gia Hưng"),
                Text("member4Role", "Vai trò 4", "Điều hành tour")
            }),
            Section("contact", "hero", "Hero liên hệ", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Liên hệ"),
                Text("title", "Tiêu đề", "Chúng tôi luôn sẵn sàng lắng nghe"),
                TextArea("description", "Mô tả", "Liên hệ với HV Travel để được tư vấn hành trình phù hợp và nhận hỗ trợ nhanh chóng.")
            }),
            Section("contact", "cards", "Thẻ liên hệ", 2, new List<ContentField>
            {
                Text("addressTitle", "Tiêu đề địa chỉ", "Địa chỉ"),
                Text("phoneTitle", "Tiêu đề điện thoại", "Điện thoại"),
                Text("emailTitle", "Tiêu đề email", "Email"),
                Text("hoursTitle", "Tiêu đề giờ làm việc", "Giờ làm việc")
            }),
            Section("contact", "formIntro", "Giới thiệu biểu mẫu liên hệ", 3, new List<ContentField>
            {
                Text("title", "Tiêu đề form", "Gửi tin nhắn cho chúng tôi"),
                TextArea("description", "Mô tả form", "Điền thông tin của bạn và đội ngũ HV Travel sẽ phản hồi trong thời gian sớm nhất."),
                Text("submitText", "Nút gửi", "Gửi tin nhắn")
            }),
            Section("publicTours", "indexHero", "Hero danh sách tour", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tour tuyển chọn"),
                Text("titleLine1", "Tiêu đề dòng 1", "Tour"),
                Text("titleHighlight", "Tiêu đề nhấn", "du lịch"),
                TextArea("description", "Mô tả", "Khám phá những hành trình nổi bật khắp Việt Nam với lịch trình được tuyển chọn kỹ lưỡng."),
                Text("searchPlaceholder", "Placeholder tìm kiếm", "Tìm điểm đến, từ khóa, thành phố..."),
                Text("searchButtonText", "Nút tìm kiếm", "Tìm kiếm")
            }),
            Section("publicTours", "collectionChips", "Chip bộ sưu tập tour", 2, new List<ContentField>
            {
                Text("domesticLabel", "Chip trong nước", "Trong nước"),
                Text("internationalLabel", "Chip quốc tế", "Quốc tế"),
                Text("premiumLabel", "Chip premium", "Premium"),
                Text("budgetLabel", "Chip tiết kiệm", "Tiết kiệm"),
                Text("dealLabel", "Chip săn deal", "Săn deal")
            }),
            Section("publicTours", "filterPanel", "Nhãn bộ lọc tour", 3, new List<ContentField>
            {
                Text("headingTitle", "Tiêu đề bộ lọc", "Bộ lọc tìm kiếm"),
                Text("regionLabel", "Nhãn khu vực", "Khu vực"),
                Text("destinationLabel", "Nhãn điểm đến", "Điểm đến"),
                Text("allOptionLabel", "Nhãn tất cả", "Tất cả"),
                Text("minPriceLabel", "Nhãn giá từ", "Giá từ"),
                Text("minPricePlaceholder", "Placeholder giá từ", "2.000.000"),
                Text("maxPriceLabel", "Nhãn giá đến", "Giá đến"),
                Text("maxPricePlaceholder", "Placeholder giá đến", "15.000.000"),
                Text("departureMonthLabel", "Nhãn tháng khởi hành", "Tháng khởi hành"),
                Text("departureMonthPrefix", "Tiền tố tháng", "Tháng"),
                Text("maxDaysLabel", "Nhãn tối đa ngày", "Tối đa ngày"),
                Text("maxDaysPlaceholder", "Placeholder tối đa ngày", "7"),
                Text("travellersLabel", "Nhãn số khách", "Số khách"),
                Text("travellersPlaceholder", "Placeholder số khách", "2"),
                Text("routeStyleLabel", "Nhãn kiểu hành trình", "Kiểu hành trình"),
                Text("routeStyleCompactLabel", "Kiểu gọn", "Gọn, ít di chuyển"),
                Text("routeStyleBalancedLabel", "Kiểu cân bằng", "Cân bằng"),
                Text("routeStyleHighlightsLabel", "Kiểu nổi bật", "Ưu tiên điểm nổi bật"),
                Text("sortLabel", "Nhãn sắp xếp", "Sắp xếp"),
                Text("sortRecommendedLabel", "Sắp xếp đề xuất", "Phù hợp nhất"),
                Text("sortRatingLabel", "Sắp xếp đánh giá", "Đánh giá cao"),
                Text("sortBestValueLabel", "Sắp xếp giá tốt", "Giá tốt nhất"),
                Text("sortPriceAscLabel", "Sắp xếp giá tăng", "Giá thấp đến cao"),
                Text("sortPriceDescLabel", "Sắp xếp giá giảm", "Giá cao đến thấp"),
                Text("sortDepartureLabel", "Sắp xếp khởi hành", "Khởi hành gần nhất"),
                Text("confirmationLabel", "Nhãn xác nhận", "Xác nhận"),
                Text("cancellationLabel", "Nhãn hủy hoàn", "Hủy / hoàn"),
                Text("availableOnlyLabel", "Lọc còn chỗ", "Chỉ hiển thị tour còn chỗ"),
                Text("promotionOnlyLabel", "Lọc khuyến mãi", "Chỉ hiển thị tour có khuyến mãi"),
                Text("summaryRegionFormat", "Tóm tắt khu vực", "Khu vực đang có: {0} lựa chọn"),
                Text("summaryConfirmationFormat", "Tóm tắt xác nhận", "Kiểu xác nhận: {0} lựa chọn"),
                Text("applyButtonText", "Nút áp dụng", "Áp dụng"),
                Text("resetButtonText", "Nút reset", "Reset")
            }),
            Section("publicTours", "resultsPanel", "Nhãn kết quả danh sách tour", 4, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow kết quả", "Khám phá"),
                Text("titleFormat", "Định dạng tiêu đề kết quả", "{0} tour phù hợp"),
                Text("routeStyleSummaryFormat", "Tóm tắt kiểu hành trình", "Phù hợp với kiểu hành trình {0}"),
                Text("wishlistEyebrow", "Eyebrow wishlist", "Danh sách yêu thích"),
                Text("wishlistTitle", "Tiêu đề wishlist", "Tour đã lưu"),
                Text("wishlistEmptyText", "Thông báo wishlist rỗng", "Chưa có tour nào trong wishlist."),
                Text("recentEyebrow", "Eyebrow vừa xem", "Đã xem gần đây"),
                Text("recentTitle", "Tiêu đề vừa xem", "Tour vừa xem"),
                Text("recentEmptyText", "Thông báo vừa xem rỗng", "Chưa có tour nào vừa xem.")
            }),
            Section("publicTours", "emptyState", "Trạng thái rỗng danh sách tour", 5, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Không tìm thấy tour nào"),
                TextArea("description", "Mô tả", "Hãy thử đổi từ khóa hoặc quay lại toàn bộ danh sách tour đang mở bán."),
                Text("ctaText", "Nút CTA", "Xem tất cả tour")
            }),
            Section("destinations", "hero", "Hero điểm đến", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Destination Hub"),
                Text("title", "Tiêu đề", "Từ điểm đến đến bộ sưu tập tour, tất cả được gom về một hub khám phá."),
                TextArea("description", "Mô tả", "Trang này hỗ trợ marketing mở landing theo vùng, theo nhu cầu và theo mức giá thay vì chỉ có danh sách tour phẳng.")
            }),
            Section("destinations", "collectionsIntro", "Mở đầu bộ sưu tập", 2, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Collections"),
                Text("title", "Tiêu đề", "Bộ sưu tập nổi bật"),
                TextArea("description", "Mô tả", "Nhóm các collection giúp marketing đẩy nhanh trang đích theo nhu cầu và mức ngân sách." )
            }),
            Section("destinations", "regionsIntro", "Mở đầu theo vùng", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Regions"),
                Text("title", "Tiêu đề", "Khám phá theo vùng"),
                TextArea("description", "Mô tả", "Trình bày điểm đến theo vùng để mở rộng landing page bán theo khu vực và nhóm thành phố nổi bật.")
            }),
            Section("promotions", "hero", "Hero khuyến mãi", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Promotion Center"),
                Text("title", "Tiêu đề", "Flash sale, chiến dịch voucher và trang deal cho HV Travel."),
                TextArea("description", "Mô tả", "Trang mới gom toàn bộ khuyến mãi, hiển thị mức ưu đãi, thời hạn và điều kiện theo phân khúc khách hàng.")
            }),
            Section("promotions", "flashSalesIntro", "Mở đầu flash sale", 2, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Flash sale"),
                Text("title", "Tiêu đề", "Khuyến mãi tạo tính cấp bách"),
                TextArea("description", "Mô tả", "Khối ưu đãi có thời hạn ngắn để kéo chuyển đổi ngay trên mặt tiền B2C.")
            }),
            Section("promotions", "voucherIntro", "Mở đầu ví voucher", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Chiến dịch voucher"),
                Text("title", "Tiêu đề", "Ví voucher"),
                TextArea("description", "Mô tả", "Tập hợp voucher và chiến dịch có thể tái sử dụng cho nhiều phân khúc khách hàng.")
            }),
            Section("promotions", "seasonalDealsIntro", "Mở đầu deal theo mùa", 4, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Seasonal deals"),
                Text("title", "Tiêu đề", "Deal theo mùa"),
                TextArea("description", "Mô tả", "Nội dung dành cho các chiến dịch theo mùa, lễ hội và khung thời gian bán hàng cao điểm.")
            }),
            Section("services", "hero", "Hero dịch vụ", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "HV Travel Services"),
                Text("title", "Tiêu đề", "Báo giá vé, khách sạn, combo, visa trong một màn hình."),
                TextArea("description", "Mô tả", "Mô hình mới theo hướng ecosystem: khách để brief nhanh, sales nhận lead ngay, portal vẫn giữ lịch sử yêu cầu và trạng thái xử lý.")
            }),
            Section("services", "serviceCards", "Nội dung thẻ dịch vụ", 2, new List<ContentField>
            {
                Text("flightTitle", "Tiêu đề Vé máy bay", "Vé máy bay"),
                TextArea("flightDescription", "Mô tả Vé máy bay", "Giữ chỗ nhanh, tối ưu lịch bay và baggage theo hành trình."),
                Text("hotelTitle", "Tiêu đề Khách sạn", "Khách sạn"),
                TextArea("hotelDescription", "Mô tả Khách sạn", "Từ city break đến resort gia đình với báo giá theo ngân sách."),
                Text("comboTitle", "Tiêu đề Combo", "Combo"),
                TextArea("comboDescription", "Mô tả Combo", "Gom vé + phòng + tour lẻ để khóa deal trọn gói."),
                Text("visaTitle", "Tiêu đề Visa", "Visa"),
                TextArea("visaDescription", "Mô tả Visa", "Checklist hồ sơ, timeline nộp và hỗ trợ tỷ lệ đậu tốt hơn.")
            }),
            Section("services", "quoteFormIntro", "Giới thiệu form báo giá", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Quick Brief"),
                Text("title", "Tiêu đề", "Yêu cầu báo giá"),
                TextArea("description", "Mô tả", "Lead được gắn SLA, phân tuyến admin và giữ lịch sử cho các đợt chăm sóc sau."),
                Text("submitText", "Nút gửi", "Gửi yêu cầu báo giá")
            }),
            Section("inspiration", "hero", "Hero cẩm nang", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Content Hub"),
                Text("title", "Tiêu đề", "Cẩm nang, visa tips, mùa lễ hội và những landing SEO có thể tự quản trị."),
                TextArea("description", "Mô tả", "Khu vực nội dung giúp đội marketing làm hub cho bài viết nổi bật, nhóm chủ đề và bài mới nhất.")
            }),
            Section("inspiration", "featuredIntro", "Mở đầu bài nổi bật", 2, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Featured"),
                Text("title", "Tiêu đề", "Bài viết nổi bật"),
                TextArea("description", "Mô tả", "Khối bài nổi bật giúp kéo traffic vào nội dung chủ lực và landing SEO quan trọng.")
            }),
            Section("inspiration", "latestIntro", "Mở đầu bài mới", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Latest stories"),
                Text("title", "Tiêu đề", "Bài viết mới nhất"),
                TextArea("description", "Mô tả", "Danh sách bài mới hỗ trợ giữ nhịp xuất bản đều và mở rộng chiều sâu cho content hub.")
            }),
            Section("inspirationDetails", "hero", "Hero chi tiết cẩm nang", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Travel story"),
                Text("metaFormat", "Định dạng meta", "{0} • HV Travel"),
                TextArea("description", "Mô tả", "Bài viết giữ nội dung động từ hệ thống, còn hero copy và helper text có thể chỉnh trong CMS.")
            }),
            Section("inspirationDetails", "body", "Thân bài chi tiết cẩm nang", 2, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Nội dung bài viết"),
                TextArea("description", "Mô tả", "Đội ngũ có thể thay đổi phần mở đầu của khu vực body mà không can thiệp nội dung bài viết gốc."),
                Text("emptyStateText", "Thông báo rỗng", "Bài viết này đang được cập nhật nội dung.")
            }),
            Section("inspirationDetails", "tags", "Tags chi tiết cẩm nang", 3, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Chủ đề liên quan"),
                Text("emptyStateText", "Thông báo rỗng", "Bài viết chưa có tag hiển thị.")
            }),
            Section("publicTourDetails", "hero", "Hero chi tiết tour", 1, new List<ContentField>
            {
                Text("instantConfirmationLabel", "Nhãn xác nhận tức thì", "Xác nhận tức thì"),
                Text("requestConfirmationLabel", "Nhãn chờ xác nhận", "Chờ xác nhận"),
                Text("freeCancellationLabel", "Nhãn hủy miễn phí", "Hủy miễn phí"),
                Text("lowAvailabilityLabel", "Nhãn còn ít chỗ", "Còn ít chỗ"),
                Text("locationFallback", "Fallback địa điểm", "Việt Nam"),
                Text("durationFallback", "Fallback thời lượng", "Liên hệ tư vấn"),
                Text("reviewFormat", "Định dạng đánh giá", "{0} đánh giá")
            }),
            Section("publicTourDetails", "highlights", "Điểm nổi bật tour", 2, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Điểm nổi bật"),
                Text("title", "Tiêu đề", "Điểm nổi bật của hành trình"),
                Text("badgeText", "Badge", "Tour bán tốt"),
                Text("emptyStateText", "Thông báo rỗng", "Tour này chưa có danh sách điểm nổi bật hiển thị.")
            }),
            Section("publicTourDetails", "overview", "Tổng quan tour", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tổng quan"),
                Text("title", "Tiêu đề", "Tổng quan hành trình"),
                Text("canonicalReadyLabel", "Nhãn canonical", "Canonical đã sẵn sàng")
            }),
            Section("publicTourDetails", "inclusions", "Bao gồm và không bao gồm", 4, new List<ContentField>
            {
                Text("includedTitle", "Tiêu đề bao gồm", "Bao gồm"),
                Text("includedEmptyText", "Thông báo bao gồm rỗng", "Thông tin dịch vụ bao gồm đang được cập nhật."),
                Text("excludedTitle", "Tiêu đề không bao gồm", "Không bao gồm"),
                Text("excludedEmptyText", "Thông báo không bao gồm rỗng", "Thông tin dịch vụ không bao gồm đang được cập nhật.")
            }),
            Section("publicTourDetails", "schedule", "Lịch trình tour", 5, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Lịch trình"),
                Text("title", "Tiêu đề", "Lịch trình chi tiết"),
                Text("emptyStateText", "Thông báo rỗng", "Lịch trình chi tiết sẽ được cập nhật gần ngày khởi hành.")
            }),
            Section("publicTourDetails", "routing", "Tổng quan lộ trình", 6, new List<ContentField>
              {
                  Text("eyebrowText", "Eyebrow", "Tổng quan lộ trình"),
                  Text("title", "Tiêu đề", "Lộ trình tham quan"),
                  TextArea("description", "Mô tả", "Các điểm dừng chính được cấu trúc lại theo từng ngày để bạn hình dung nhịp di chuyển của hành trình."),
                  Text("dayCountFormat", "Định dạng số ngày", "{0} ngày"),
                  Text("stopCountFormat", "Định dạng số điểm dừng", "{0} điểm dừng"),
                  Text("visitMinutesFormat", "Định dạng tổng phút tham quan", "{0} phút tham quan"),
                Text("travelMinutesFormat", "Định dạng tổng phút di chuyển", "{0} phút di chuyển"),
                Text("journeyMinutesFormat", "Định dạng tổng thời lượng hành trình", "{0} phút hành trình"),
                Text("distanceFormat", "Định dạng quãng đường", "{0} km"),
                Text("dayTravelMinutesFormat", "Định dạng phút di chuyển theo ngày", "{0} phút di chuyển"),
                Text("dayJourneyMinutesFormat", "Định dạng thời lượng hành trình theo ngày", "{0} phút hành trình"),
                Text("transferTimeFormat", "Định dạng thời gian trung chuyển", "Di chuyển khoảng {0} phút"),
                Text("dayPartLabel", "Nhãn khung giờ", "Khung giờ"),
                Text("congestionLabel", "Nhãn mật độ giao thông", "Mật độ giao thông"),
                Text("junctionDelayLabel", "Nhãn độ trễ giao lộ", "Độ trễ giao lộ"),
                Text("typeLabel", "Nhãn loại điểm", "Loại điểm")
            }),
            Section("publicTourDetails", "policies", "Chính sách tour", 7, new List<ContentField>
            {
                Text("cancellationEyebrowText", "Eyebrow hủy", "Hủy tour"),
                Text("cancellationTitle", "Tiêu đề hủy", "Chính sách hủy và hoàn"),
                Text("cancellationSummaryFallback", "Fallback tóm tắt", "Liên hệ tư vấn để kiểm tra chính sách theo từng lịch khởi hành."),
                Text("freeCancellationFormat", "Định dạng hủy miễn phí", "Miễn phí hủy trước {0} giờ."),
                TextArea("cancellationDescriptionFallback", "Fallback mô tả hủy", "Điều kiện hủy và hoàn tiền được áp dụng theo quy định của tour và từng lịch khởi hành."),
                Text("meetingPointEyebrowText", "Eyebrow điểm đón", "Điểm hẹn"),
                Text("meetingPointTitle", "Tiêu đề điểm đón", "Điểm đón và hướng dẫn check-in"),
                Text("meetingPointFallback", "Fallback điểm đón", "HV Travel sẽ gửi điểm hẹn chi tiết trong voucher xác nhận."),
                TextArea("meetingPointDescription", "Mô tả điểm đón", "Thông tin hotline, giờ có mặt và hướng dẫn check-in sẽ được cập nhật rõ trong dòng thời gian booking.")
            }),
            Section("publicTourDetails", "departures", "Lịch khởi hành tour", 8, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Lịch giá"),
                Text("title", "Tiêu đề", "Lịch khởi hành và chỗ trống"),
                Text("countFormat", "Định dạng số đợt", "{0} đợt khởi hành"),
                Text("departureColumnLabel", "Nhãn cột departure", "Khởi hành"),
                Text("adultPriceColumnLabel", "Nhãn cột người lớn", "Người lớn"),
                Text("policyColumnLabel", "Nhãn cột chính sách", "Chính sách"),
                Text("seatsColumnLabel", "Nhãn cột số chỗ", "Số chỗ"),
                Text("instantConfirmationLabel", "Nhãn xác nhận tức thì", "Xác nhận tức thì"),
                Text("requestConfirmationLabel", "Nhãn chờ xác nhận", "Chờ xác nhận"),
                Text("freeCancellationLabel", "Nhãn hủy miễn phí", "Hủy miễn phí"),
                Text("conditionalPolicyLabel", "Nhãn theo điều kiện", "Theo điều kiện tour"),
                Text("seatsRemainingFormat", "Định dạng số chỗ còn lại", "{0} còn lại")
            }),
            Section("publicTourDetails", "bookingPanel", "Khối đặt tour", 9, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Sẵn sàng đặt tour"),
                TextArea("priceHelperText", "Mô tả giá", "Mức giá tốt nhất theo lịch khởi hành hiện có."),
                Text("confirmationLabel", "Nhãn xác nhận", "Xác nhận"),
                Text("ratingLabel", "Nhãn đánh giá", "Đánh giá"),
                Text("departureLabel", "Nhãn departure", "Ngày khởi hành"),
                Text("bookNowText", "CTA đặt tour", "Đặt tour ngay"),
                Text("callAdvisorText", "CTA gọi tư vấn", "Gọi tư vấn"),
                Text("trustEyebrowText", "Eyebrow trust", "Khối tin cậy"),
                Text("supportTitle", "Tiêu đề hỗ trợ", "Hỗ trợ sau khi đặt tour"),
                TextArea("supportDescription", "Mô tả hỗ trợ", "Cổng khách hàng, mốc thanh toán và tra cứu booking đã sẵn sàng sau checkout."),
                Text("voucherTitle", "Tiêu đề voucher", "Voucher và hóa đơn"),
                TextArea("voucherDescription", "Mô tả voucher", "Sau khi thanh toán, booking có thể cập nhật voucher, hóa đơn và trạng thái thực hiện theo chính sách.")
            }),
            Section("publicTourDetails", "relatedTours", "Tour liên quan", 10, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Tour liên quan"),
                Text("emptyStateText", "Thông báo rỗng", "Chưa có tour liên quan phù hợp để hiển thị.")
            }),
            Section("booking", "consultationHero", "Hero tư vấn", 1, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Tư vấn chuyến đi theo cách của bạn"),
                TextArea("description", "Mô tả", "Chia sẻ nhu cầu để đội ngũ HV Travel tư vấn tour phù hợp với lịch trình và ngân sách của bạn.")
            }),
            Section("booking", "consultationBenefits", "Lợi ích tư vấn", 2, new List<ContentField>
            {
                Text("quickContactTitle", "Tiêu đề liên hệ nhanh", "Liên hệ nhanh"),
                Text("hotlineLabel", "Nhãn hotline", "Hotline"),
                Text("contactEmailLabel", "Nhãn email liên hệ", "Email"),
                Text("businessHoursTitle", "Tiêu đề giờ làm việc", "Giờ làm việc"),
                Text("supportScheduleLabel", "Nhãn lịch hỗ trợ", "Lịch hỗ trợ"),
                Text("reasonsTitle", "Tiêu đề lý do chọn", "Tại sao chọn HV Travel?"),
                Text("reason1Text", "Lý do 1", "10+ năm kinh nghiệm tổ chức tour"),
                Text("reason2Text", "Lý do 2", "Đội ngũ hướng dẫn viên chuyên nghiệp"),
                Text("reason3Text", "Lý do 3", "Tư vấn miễn phí, không ràng buộc"),
                Text("reason4Text", "Lý do 4", "Cam kết giá tốt nhất thị trường"),
                Text("formTitle", "Tiêu đề form", "Gửi yêu cầu tư vấn"),
                Text("fullNameLabel", "Nhãn họ và tên", "Họ và tên"),
                Text("fullNamePlaceholder", "Placeholder họ và tên", "Nguyễn Văn A"),
                Text("phoneLabel", "Nhãn số điện thoại", "Số điện thoại"),
                Text("phonePlaceholder", "Placeholder số điện thoại", "0901 234 567"),
                Text("formEmailLabel", "Nhãn email form", "Email"),
                Text("emailPlaceholder", "Placeholder email", "email@example.com"),
                Text("tourInterestLabel", "Nhãn tour quan tâm", "Tour quan tâm"),
                Text("tourInterestPlaceholder", "Placeholder tour quan tâm", "VD: Tour Đà Nẵng 3N2Đ, Tour Phú Quốc..."),
                Text("preferredContactTimeLabel", "Nhãn thời gian liên hệ", "Thời gian liên hệ mong muốn"),
                Text("preferredMorningLabel", "Nhãn liên hệ buổi sáng", "Sáng (8-12h)"),
                Text("preferredAfternoonLabel", "Nhãn liên hệ buổi chiều", "Chiều (13-17h)"),
                Text("preferredEveningLabel", "Nhãn liên hệ buổi tối", "Tối (18-21h)"),
                Text("messageLabel", "Nhãn nội dung yêu cầu", "Nội dung yêu cầu"),
                Text("messagePlaceholder", "Placeholder nội dung yêu cầu", "Mô tả yêu cầu của bạn: số người, ngân sách dự kiến, ngày đi mong muốn..."),
                Text("submitText", "Nút gửi", "Gửi yêu cầu"),
                Text("responseSlaText", "Thông báo thời gian phản hồi", "Chúng tôi sẽ phản hồi trong vòng 24 giờ làm việc"),
                Text("successTitle", "Tiêu đề thành công", "Gửi yêu cầu thành công!"),
                TextArea("successDescription", "Mô tả thành công", "Chúng tôi đã nhận được thông tin và sẽ liên hệ với bạn trong thời gian sớm nhất."),
                Text("exploreToursText", "Nút khám phá tour", "Khám phá tour")
            }),
            Section("booking", "statusCopy", "Nội dung trạng thái đặt tour", 3, new List<ContentField>
            {
                Text("successTitle", "Tiêu đề thành công", "Đặt tour thành công!"),
                TextArea("successDescription", "Mô tả thành công", "Cảm ơn bạn đã đặt tour tại HV Travel. Chúng tôi sẽ liên hệ xác nhận trong thời gian sớm nhất."),
                Text("failedTitle", "Tiêu đề thất bại", "Thanh toán thất bại"),
                TextArea("failedDescription", "Mô tả thất bại", "Thanh toán không thành công. Vui lòng thử lại hoặc chọn phương thức khác."),
                Text("errorTitle", "Tiêu đề lỗi", "Đã xảy ra lỗi"),
                TextArea("errorDescription", "Mô tả lỗi", "Đã xảy ra lỗi hệ thống trong quá trình xử lý. Vui lòng thử lại sau.")
            }),
            Section("booking", "createHero", "Hero tạo booking", 4, new List<ContentField>
            {
                Text("badgeText", "Badge", "Bước 1 trong hành trình đặt tour"),
                Text("title", "Tiêu đề", "Đặt tour và xác nhận hành khách"),
                TextArea("description", "Mô tả", "Chọn lịch khởi hành phù hợp, kiểm tra báo giá theo thời gian thực và xác nhận thông tin liên hệ trước khi chuyển sang thanh toán.")
            }),
            Section("booking", "createStepper", "Stepper tạo booking", 5, new List<ContentField>
            {
                Text("detailsStepLabel", "Bước 1", "Thông tin"),
                Text("paymentStepLabel", "Bước 2", "Thanh toán"),
                Text("completeStepLabel", "Bước 3", "Hoàn tất")
            }),
            Section("booking", "travellerForm", "Form hành khách", 6, new List<ContentField>
            {
                Text("tourSnapshotEyebrow", "Eyebrow tour snapshot", "Thông tin tour"),
                Text("tourFallbackDestination", "Fallback điểm đến", "Việt Nam"),
                Text("tourFallbackDuration", "Fallback thời lượng", "Liên hệ tư vấn"),
                Text("viewDetailsText", "CTA xem chi tiết", "Xem chi tiết"),
                Text("tourInfoTitle", "Tiêu đề thông tin tour", "Thông tin tour"),
                Text("departureEyebrow", "Eyebrow departure", "Đợt khởi hành"),
                Text("departureTitle", "Tiêu đề departure", "Chọn đợt khởi hành"),
                Text("remainingCapacityFormat", "Định dạng còn chỗ", "{0} chỗ"),
                Text("instantConfirmationLabel", "Nhãn xác nhận tức thì", "Xác nhận tức thì"),
                Text("requestConfirmationLabel", "Nhãn chờ xác nhận", "Chờ xác nhận"),
                Text("cutoffFormat", "Định dạng cutoff", "cutoff {0} giờ"),
                Text("adultLabel", "Nhãn người lớn", "Người lớn"),
                Text("childLabel", "Nhãn trẻ em", "Trẻ em"),
                Text("infantLabel", "Nhãn em bé", "Em bé"),
                Text("freeCancellationFormat", "Định dạng hủy miễn phí", "Hủy miễn phí trước {0} giờ."),
                Text("cancellationFallback", "Fallback chính sách hủy", "Chính sách hủy áp dụng theo điều kiện của tour."),
                Text("departureEmptyText", "Thông báo không có departure", "Tour này hiện chưa có lịch khởi hành khả dụng."),
                Text("travellerEyebrow", "Eyebrow hành khách", "Hành khách"),
                Text("travellerTitle", "Tiêu đề hành khách", "Thông tin liên hệ và số khách"),
                Text("contactNameLabel", "Nhãn họ tên", "Họ và tên"),
                Text("contactNamePlaceholder", "Placeholder họ tên", "Nguyễn Văn A"),
                Text("contactEmailLabel", "Nhãn email", "Email"),
                Text("contactEmailPlaceholder", "Placeholder email", "email@example.com"),
                Text("contactPhoneLabel", "Nhãn số điện thoại", "Số điện thoại"),
                Text("contactPhonePlaceholder", "Placeholder số điện thoại", "0901 234 567"),
                Text("specialRequestsLabel", "Nhãn yêu cầu đặc biệt", "Yêu cầu đặc biệt"),
                Text("specialRequestsPlaceholder", "Placeholder yêu cầu đặc biệt", "Ví dụ: ăn chay, ghế gần cửa sổ, hỗ trợ xe đón...")
            }),
            Section("booking", "pricingPanel", "Panel báo giá", 7, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tùy chọn thanh toán"),
                Text("title", "Tiêu đề", "Coupon và kế hoạch thanh toán"),
                Text("couponLabel", "Nhãn coupon", "Mã ưu đãi"),
                Text("couponPlaceholder", "Placeholder coupon", "Nhập nếu bạn có mã ưu đãi"),
                Text("paymentPlanLabel", "Nhãn payment plan", "Hình thức thanh toán"),
                Text("paymentPlanFullLabel", "Nhãn full", "Thanh toán toàn bộ"),
                Text("paymentPlanDepositLabel", "Nhãn deposit", "Đặt cọc 30%"),
                Text("quoteEyebrowText", "Eyebrow báo giá", "Báo giá"),
                Text("quoteTitle", "Tiêu đề báo giá", "Xem báo giá trước khi giữ chỗ"),
                Text("subtotalLabel", "Nhãn tạm tính", "Tạm tính"),
                Text("discountLabel", "Nhãn giảm giá", "Giảm giá"),
                Text("grandTotalLabel", "Nhãn tổng thanh toán", "Tổng thanh toán"),
                Text("amountDueNowLabel", "Nhãn thanh toán ngay", "Thanh toán ngay"),
                Text("balanceDueLabel", "Nhãn còn lại", "Còn lại"),
                TextArea("quoteStatusText", "Thông báo trạng thái", "Báo giá sẽ tự cập nhật khi bạn đổi ngày khởi hành, số khách, mã ưu đãi hoặc hình thức thanh toán."),
                Text("submitText", "CTA tiếp tục", "Tiếp tục sang thanh toán"),
                TextArea("submitHelperText", "Mô tả CTA", "Ghế chỉ được giữ khi máy chủ xác nhận báo giá và tạo booking thành công."),
                Text("couponAppliedFormat", "Định dạng coupon đã áp dụng", "Đã áp dụng mã ưu đãi {0}."),
                Text("quoteReadyText", "Thông báo báo giá hợp lệ", "Báo giá hợp lệ, có thể tiếp tục giữ chỗ.")
            }),
            Section("booking", "paymentHero", "Hero thanh toán", 8, new List<ContentField>
            {
                Text("badgeText", "Badge", "Bước 2 trong hành trình đặt tour"),
                Text("title", "Tiêu đề", "Xác nhận thanh toán booking"),
                TextArea("description", "Mô tả", "Chọn phương thức thanh toán, tải minh chứng nếu chuyển khoản và tiếp tục từ phiên checkout hiện tại khi cần.")
            }),
            Section("booking", "paymentStepper", "Stepper thanh toán", 9, new List<ContentField>
            {
                Text("detailsStepLabel", "Bước 1", "Thông tin"),
                Text("paymentStepLabel", "Bước 2", "Thanh toán"),
                Text("completeStepLabel", "Bước 3", "Hoàn tất")
            }),
            Section("booking", "paymentMethods", "Phương thức thanh toán", 10, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Phương thức thanh toán"),
                Text("title", "Tiêu đề", "Chọn cách thu tiền cho booking này"),
                Text("bankTransferTitle", "Phương thức chuyển khoản", "Chuyển khoản thủ công"),
                TextArea("bankTransferDescription", "Mô tả chuyển khoản", "Tải minh chứng chuyển khoản để đội vận hành đối soát trên admin."),
                Text("creditCardTitle", "Phương thức online", "Thanh toán online"),
                TextArea("creditCardDescription", "Mô tả online", "Luồng demo nội bộ sẽ gọi callback idempotent để mô phỏng gateway online."),
                Text("cashTitle", "Phương thức tiền mặt", "Giữ chỗ thanh toán tiền mặt"),
                TextArea("cashDescription", "Mô tả tiền mặt", "Giữ chỗ trước và thanh toán bằng tiền mặt tại văn phòng hoặc khi khởi hành."),
                Text("submitText", "CTA xác nhận", "Xác nhận thanh toán")
            }),
            Section("booking", "transferProof", "Minh chứng chuyển khoản", 11, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Minh chứng chuyển khoản"),
                Text("title", "Tiêu đề", "Tải minh chứng chuyển khoản"),
                Text("resumeText", "CTA resume", "Khôi phục checkout"),
                Text("notePlaceholder", "Placeholder note", "Ghi chú đối soát hoặc mã tham chiếu giao dịch"),
                Text("submitText", "CTA gửi minh chứng", "Gửi minh chứng")
            }),
            Section("booking", "orderSummary", "Tóm tắt đơn", 12, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tóm tắt đơn hàng"),
                Text("titleFormat", "Định dạng tiêu đề", "Đơn đặt {0}"),
                Text("bookingCodeLabel", "Nhãn mã đơn", "Mã đơn"),
                Text("departureLabel", "Nhãn khởi hành", "Khởi hành"),
                Text("couponLabel", "Nhãn coupon", "Mã ưu đãi"),
                Text("paymentStatusLabel", "Nhãn trạng thái thanh toán", "Trạng thái thanh toán"),
                Text("subtotalLabel", "Nhãn tạm tính", "Tạm tính"),
                Text("discountLabel", "Nhãn giảm giá", "Giảm giá"),
                Text("grandTotalLabel", "Nhãn tổng thanh toán", "Tổng thanh toán"),
                Text("paymentPlanLabel", "Nhãn kế hoạch", "Kế hoạch"),
                Text("amountDueNowLabel", "Nhãn thanh toán ngay", "Thanh toán ngay"),
                Text("balanceDueLabel", "Nhãn còn lại", "Còn lại"),
                Text("checkoutSessionLabel", "Nhãn checkout session", "Phiên checkout"),
                Text("paymentSessionLabel", "Nhãn payment session", "Phiên thanh toán"),
                Text("pendingLabel", "Fallback pending", "Đang chờ xử lý")
            }),
            Section("booking", "paymentTimeline", "Timeline thanh toán", 13, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tiến trình"),
                Text("title", "Tiêu đề", "Tiến trình thanh toán"),
                Text("emptyStateText", "Thông báo rỗng", "Booking chưa có mốc xử lý hiển thị cho khách hàng.")
            }),
            Section("customerLogin", "hero", "Hero đăng nhập", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tài khoản HV Travel"),
                Text("title", "Tiêu đề", "Đăng nhập để tiếp tục hành trình của bạn."),
                TextArea("description", "Mô tả", "Sử dụng email khách hàng để xem thông tin tài khoản, theo dõi trạng thái đặt tour và giữ mọi trải nghiệm du lịch ở cùng một nơi.")
            }),
            Section("customerLogin", "featureCards", "Thẻ tính năng đăng nhập", 2, new List<ContentField>
            {
                Text("card1Value", "Giá trị thẻ 1", "24/7"),
                Text("card1Description", "Mô tả thẻ 1", "Hỗ trợ cập nhật booking mọi lúc"),
                Text("card2Value", "Giá trị thẻ 2", "1 tài khoản"),
                Text("card2Description", "Mô tả thẻ 2", "Quản lý email, hồ sơ và điểm thưởng"),
                Text("card3Value", "Giá trị thẻ 3", "Bảo mật"),
                Text("card3Description", "Mô tả thẻ 3", "Mật khẩu đang được mã hóa theo chuẩn hash")
            }),
            Section("customerLogin", "formIntro", "Giới thiệu form đăng nhập", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Chào mừng trở lại"),
                Text("title", "Tiêu đề", "Đăng nhập khách hàng"),
                TextArea("description", "Mô tả", "Ví dụ tài khoản hợp lệ theo dữ liệu hiện có: hhunu114@gmail.com"),
                Text("emailLabel", "Nhãn email", "Email"),
                Text("emailPlaceholder", "Placeholder email", "email@vidu.com"),
                Text("passwordLabel", "Nhãn mật khẩu", "Mật khẩu"),
                Text("passwordPlaceholder", "Placeholder mật khẩu", "Nhập mật khẩu của bạn"),
                Text("forgotPasswordText", "CTA quên mật khẩu", "Quên mật khẩu?"),
                Text("rememberMeLabel", "Nhãn ghi nhớ", "Ghi nhớ đăng nhập"),
                Text("cookieHint", "Gợi ý cookie", "Cookie tối đa 14 ngày"),
                Text("submitText", "CTA đăng nhập", "Đăng nhập")
            }),
            Section("customerLogin", "registerPrompt", "CTA đăng ký", 4, new List<ContentField>
            {
                TextArea("title", "Thông điệp", "Chưa có tài khoản? Tạo mới để lưu lịch sử đặt tour và hồ sơ liên hệ."),
                Text("actionText", "CTA", "Đăng ký ngay")
            }),
            Section("customerRegister", "hero", "Hero đăng ký", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tham gia HV Travel"),
                Text("title", "Tiêu đề", "Tạo tài khoản để bắt đầu những chuyến đi mới."),
                TextArea("description", "Mô tả", "Đăng ký một lần để lưu hồ sơ liên hệ, nhận ưu đãi riêng và quản lý toàn bộ lịch sử đặt tour trên cùng một giao diện.")
            }),
            Section("customerRegister", "benefits", "Quyền lợi đăng ký", 2, new List<ContentField>
            {
                Text("sectionTitle", "Tiêu đề khối", "Quyền lợi"),
                Text("sectionDescription", "Mô tả khối", "Theo dõi booking và thông tin khách hàng tập trung"),
                Text("card1Title", "Tiêu đề thẻ 1", "Nhanh"),
                Text("card1Description", "Mô tả thẻ 1", "Đăng ký trong vài phút với thông tin cơ bản"),
                Text("card2Title", "Tiêu đề thẻ 2", "Minh bạch"),
                Text("card2Description", "Mô tả thẻ 2", "Tài khoản mới được tạo ở trạng thái hoạt động ngay")
            }),
            Section("customerRegister", "formIntro", "Giới thiệu form đăng ký", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Tạo tài khoản"),
                Text("title", "Tiêu đề", "Đăng ký khách hàng"),
                TextArea("description", "Mô tả", "Giao diện này được thiết kế đồng bộ với trang đăng nhập để người dùng có trải nghiệm liền mạch."),
                Text("fullNameLabel", "Nhãn họ tên", "Họ và tên"),
                Text("fullNamePlaceholder", "Placeholder họ tên", "Nguyễn Văn A"),
                Text("emailLabel", "Nhãn email", "Email"),
                Text("emailPlaceholder", "Placeholder email", "email@vidu.com"),
                Text("phoneLabel", "Nhãn điện thoại", "Số điện thoại"),
                Text("phonePlaceholder", "Placeholder điện thoại", "09xxxxxxxx"),
                Text("passwordLabel", "Nhãn mật khẩu", "Mật khẩu"),
                Text("passwordPlaceholder", "Placeholder mật khẩu", "Tối thiểu 6 ký tự"),
                Text("confirmPasswordLabel", "Nhãn xác nhận mật khẩu", "Xác nhận mật khẩu"),
                Text("confirmPasswordPlaceholder", "Placeholder xác nhận mật khẩu", "Nhập lại mật khẩu"),
                Text("streetLabel", "Nhãn địa chỉ", "Địa chỉ"),
                Text("streetPlaceholder", "Placeholder địa chỉ", "Số nhà, đường, phường/xã"),
                Text("cityLabel", "Nhãn thành phố", "Thành phố"),
                Text("cityPlaceholder", "Placeholder thành phố", "Hà Nội"),
                Text("countryLabel", "Nhãn quốc gia", "Quốc gia"),
                Text("countryPlaceholder", "Placeholder quốc gia", "Việt Nam"),
                TextArea("acceptTermsText", "Điều khoản", "Tôi đồng ý với điều khoản sử dụng và cho phép HV Travel lưu thông tin để phục vụ quá trình tư vấn, đặt tour."),
                Text("submitText", "CTA đăng ký", "Tạo tài khoản")
            }),
            Section("customerRegister", "loginPrompt", "CTA đăng nhập", 4, new List<ContentField>
            {
                TextArea("title", "Thông điệp", "Đã có tài khoản? Đăng nhập để tiếp tục đặt tour và quản lý hành trình."),
                Text("actionText", "CTA", "Đi đến đăng nhập")
            }),
            Section("customerPortal", "hero", "Hero customer portal", 1, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Customer Portal"),
                Text("greetingFormat", "Định dạng lời chào", "Xin chào {0}."),
                TextArea("description", "Mô tả", "Portal mới gom booking, thanh toán, loyalty, voucher, traveller profile và yêu cầu hậu mãi về cùng một nơi."),
                Text("tierLabel", "Nhãn tier", "Tier hiện tại"),
                Text("pointsFormat", "Định dạng điểm", "{0} điểm • chi tiêu {1}₫")
            }),
            Section("customerPortal", "stats", "Thống kê customer portal", 2, new List<ContentField>
            {
                Text("loyaltyLabel", "Nhãn điểm thưởng", "Điểm thưởng"),
                Text("voucherLabel", "Nhãn voucher", "Voucher khả dụng"),
                Text("travellerLabel", "Nhãn hồ sơ hành khách", "Hồ sơ hành khách"),
                Text("notificationLabel", "Nhãn thông báo", "Thông báo chưa đọc")
            }),
            Section("customerPortal", "bookingPanel", "Khối booking portal", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Dòng thời gian booking"),
                Text("title", "Tiêu đề", "Lịch sử booking"),
                Text("upcomingFormat", "Định dạng sắp khởi hành", "Sắp khởi hành: {0}"),
                Text("tourFallbackTitle", "Fallback tour", "Hành trình HV Travel"),
                Text("departurePrefix", "Tiền tố khởi hành", "Khởi hành"),
                Text("departureFallback", "Fallback khởi hành", "Đang cập nhật"),
                Text("paymentPrefix", "Tiền tố thanh toán", "Thanh toán"),
                Text("statusPrefix", "Tiền tố trạng thái", "Trạng thái"),
                Text("totalLabel", "Nhãn tổng tiền", "Tổng tiền"),
                Text("cancelActionText", "CTA hủy", "Yêu cầu hủy"),
                Text("cancelReason", "Lý do hủy mặc định", "Khách yêu cầu tự phục vụ từ portal"),
                Text("emptyStateText", "Thông báo rỗng", "Khách hàng này chưa có booking hiển thị trên portal.")
            }),
            Section("customerPortal", "reviewPanel", "Khối review portal", 4, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Đánh giá đã xác thực"),
                Text("title", "Tiêu đề", "Đánh giá chờ gửi"),
                Text("commentPlaceholder", "Placeholder đánh giá", "Chia sẻ cảm nhận của bạn"),
                Text("submitText", "CTA gửi đánh giá", "Gửi đánh giá"),
                Text("emptyStateText", "Thông báo rỗng", "Hiện chưa có chuyến đi đủ điều kiện gửi review mới.")
            }),
            Section("customerPortal", "voucherPanel", "Khối voucher portal", 5, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Ví voucher"),
                Text("title", "Tiêu đề", "Ưu đãi cá nhân hóa"),
                Text("emptyStateText", "Thông báo rỗng", "Ví voucher chưa có item khả dụng.")
            }),
            Section("customerPortal", "travellerPanel", "Khối traveller portal", 6, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Traveller profiles"),
                Text("title", "Tiêu đề", "Lưu hồ sơ hành khách"),
                Text("fullNamePlaceholder", "Placeholder họ tên", "Họ tên"),
                Text("passportPlaceholder", "Placeholder hộ chiếu", "Số hộ chiếu"),
                Text("nationalityPlaceholder", "Placeholder quốc tịch", "Quốc tịch"),
                Text("defaultCheckboxLabel", "Nhãn checkbox mặc định", "Đặt làm hồ sơ mặc định"),
                Text("submitText", "CTA lưu hồ sơ", "Lưu hồ sơ")
            }),
            Section("customerPortal", "notificationsPanel", "Khối thông báo portal", 7, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Notifications"),
                Text("title", "Tiêu đề", "Thông báo gần đây"),
                Text("emptyStateText", "Thông báo rỗng", "Hiện chưa có thông báo mới.")
            }),
            Section("bookingLookup", "hero", "Hero tra cứu booking", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tự tra cứu đơn đặt tour"),
                Text("title", "Tiêu đề", "Tra cứu booking trong vài giây"),
                TextArea("description", "Mô tả", "Nhập mã booking cùng email hoặc số điện thoại để xem tình trạng đơn, thanh toán và lịch khởi hành.")
            }),
            Section("bookingLookup", "lookupForm", "Giới thiệu form tra cứu", 2, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Thông tin tra cứu"),
                TextArea("description", "Mô tả", "Điền mã booking cùng email hoặc số điện thoại đã dùng khi đặt tour để kiểm tra nhanh trạng thái đơn."),
                Text("bookingCodeLabel", "Nhãn mã booking", "Mã booking"),
                Text("bookingCodePlaceholder", "Placeholder mã booking", "Ví dụ: HV20260328001"),
                Text("emailLabel", "Nhãn email", "Email"),
                Text("emailPlaceholder", "Placeholder email", "email@example.com"),
                Text("phoneLabel", "Nhãn số điện thoại", "Hoặc số điện thoại"),
                Text("phonePlaceholder", "Placeholder số điện thoại", "0901 234 567"),
                Text("submitText", "Nút gửi", "Tra cứu booking")
            }),
            Section("bookingLookup", "readyState", "Trạng thái sẵn sàng", 3, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Sẵn sàng tra cứu"),
                TextArea("description", "Mô tả", "Sau khi nhập đúng mã booking và thông tin liên hệ, bạn sẽ thấy trạng thái đơn và mốc xử lý mới nhất ngay tại đây."),
                Text("resultEyebrowText", "Eyebrow kết quả", "Kết quả booking"),
                Text("bookingStatusLabel", "Nhãn trạng thái đơn", "Trạng thái đơn"),
                Text("startDateLabel", "Nhãn ngày khởi hành", "Ngày khởi hành"),
                Text("startDateFallback", "Fallback ngày khởi hành", "Chưa xác định"),
                Text("participantsCountLabel", "Nhãn số hành khách", "Số hành khách"),
                Text("totalAmountLabel", "Nhãn tổng tiền", "Tổng tiền"),
                Text("timelineTitle", "Tiêu đề timeline", "Timeline xử lý"),
                Text("timelineEmptyText", "Thông báo timeline rỗng", "Booking chưa có mốc xử lý chi tiết.")
            })
        };
    }

    public static List<ContentSection> CreateSectionsForPage(string pageKey)
    {
        return CreateAllSections()
            .Where(section => section.PageKey == pageKey)
            .OrderBy(section => section.DisplayOrder)
            .ToList();
    }

    private static SiteSettingsGroup Group(string key, string title, int displayOrder, List<ContentField> fields)
    {
        return new SiteSettingsGroup
        {
            GroupKey = key,
            Title = title,
            Description = title,
            DisplayOrder = displayOrder,
            Fields = fields
        };
    }

    private static ContentSection Section(string pageKey, string sectionKey, string title, int displayOrder, List<ContentField> fields)
    {
        return new ContentSection
        {
            PageKey = pageKey,
            SectionKey = sectionKey,
            Title = title,
            Description = title,
            DisplayOrder = displayOrder,
            Fields = fields
        };
    }

    private static ContentField Text(string key, string label, string value)
    {
        return new ContentField { Key = key, Label = label, Value = value, FieldType = "text" };
    }

    private static ContentField TextArea(string key, string label, string value)
    {
        return new ContentField { Key = key, Label = label, Value = value, FieldType = "textarea" };
    }

    private static ContentField Url(string key, string label, string value)
    {
        return new ContentField { Key = key, Label = label, Value = value, FieldType = "url" };
    }
}







