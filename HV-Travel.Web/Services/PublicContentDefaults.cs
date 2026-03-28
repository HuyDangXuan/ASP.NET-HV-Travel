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
        new() { Key = "destinations", Label = "Điểm đến", Description = "Hero, bộ sưu tập nổi bật, khám phá theo vùng" },
        new() { Key = "promotions", Label = "Khuyến mãi", Description = "Hero, flash sale, voucher, deal theo mùa" },
        new() { Key = "services", Label = "Dịch vụ", Description = "Hero, thẻ dịch vụ, form báo giá" },
        new() { Key = "inspiration", Label = "Cẩm nang", Description = "Hero, bài nổi bật, danh sách bài mới" },
        new() { Key = "booking", Label = "Đặt tour", Description = "Hero tư vấn, lợi ích, nội dung trạng thái" },
        new() { Key = "bookingLookup", Label = "Tra cứu booking", Description = "Hero, form tra cứu, trạng thái sẵn sàng" }
    };

    public static Dictionary<string, List<string>> Inventory => new()
    {
        ["site"] = new() { "header", "footerBrand", "footerExplore", "footerCompany", "contactInfo", "socialLinks", "seo" },
        ["home"] = new() { "hero", "stats", "featuredToursIntro", "commitments", "finalCta" },
        ["about"] = new() { "hero", "story", "missionVision", "team" },
        ["contact"] = new() { "hero", "cards", "formIntro" },
        ["publicTours"] = new() { "indexHero", "emptyState" },
        ["destinations"] = new() { "hero", "collectionsIntro", "regionsIntro" },
        ["promotions"] = new() { "hero", "flashSalesIntro", "voucherIntro", "seasonalDealsIntro" },
        ["services"] = new() { "hero", "serviceCards", "quoteFormIntro" },
        ["inspiration"] = new() { "hero", "featuredIntro", "latestIntro" },
        ["booking"] = new() { "consultationHero", "consultationBenefits", "statusCopy" },
        ["bookingLookup"] = new() { "hero", "lookupForm", "readyState" }
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
                    Text("navAboutLabel", "Nhãn giới thiệu", "Giới thiệu"),
                    Text("navContactLabel", "Nhãn liên hệ", "Liên hệ"),
                    Text("registerLabel", "Nút đăng ký", "Đăng ký"),
                    Text("loginLabel", "Nút đăng nhập", "Đăng nhập")
                }),
                Group("footerBrand", "Thương hiệu footer", 2, new List<ContentField>
                {
                    Text("title", "Tiêu đề", "HV Travel"),
                    Text("tagline", "Khẩu hiệu", "Khám phá Việt Nam"),
                    TextArea("description", "Mô tả footer", "Khám phá vẻ đẹp Việt Nam với những tour du lịch được thiết kế riêng. Trải nghiệm văn hóa, ẩm thực và thiên nhiên tuyệt vời.")
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
                    Text("termsLabel", "Link điều khoản", "Điều khoản sử dụng")
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
            Section("home", "stats", "Chỉ số marketing trang chủ", 2, new List<ContentField>
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
            Section("home", "featuredToursIntro", "Giới thiệu tour nổi bật trang chủ", 3, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tour nổi bật"),
                Text("title", "Tiêu đề", "Hành trình được yêu thích nhất"),
                TextArea("description", "Mô tả", "Những tour du lịch hàng đầu được đánh giá cao bởi hàng ngàn du khách"),
                Text("viewAllText", "Nút xem tất cả", "Xem tất cả tour")
            }),
            Section("home", "commitments", "Cam kết trang chủ", 4, new List<ContentField>
            {
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
            Section("home", "finalCta", "CTA cuối trang chủ", 5, new List<ContentField>
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
                Text("titleLine1", "Tiêu đề dòng 1", "Tour"),
                Text("titleHighlight", "Tiêu đề nhấn", "du lịch"),
                TextArea("description", "Mô tả", "Khám phá những hành trình nổi bật khắp Việt Nam với lịch trình được tuyển chọn kỹ lưỡng."),
                Text("searchPlaceholder", "Placeholder tìm kiếm", "Tìm điểm đến, từ khóa, thành phố...")
            }),
            Section("publicTours", "emptyState", "Trạng thái rỗng danh sách tour", 2, new List<ContentField>
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
                Text("title", "Tiêu đề", "Flash sale, voucher campaign và deal landing page cho HV Travel."),
                TextArea("description", "Mô tả", "Trang mới gom toàn bộ khuyến mãi, hiển thị mức ưu đãi, thời hạn và điều kiện theo segment.")
            }),
            Section("promotions", "flashSalesIntro", "Mở đầu flash sale", 2, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Flash sale"),
                Text("title", "Tiêu đề", "Khuyến mãi tạo urgency"),
                TextArea("description", "Mô tả", "Khối ưu đãi có thời hạn ngắn để kéo chuyển đổi ngay trên mặt tiền B2C.")
            }),
            Section("promotions", "voucherIntro", "Mở đầu ví voucher", 3, new List<ContentField>
            {
                Text("eyebrowText", "Eyebrow", "Voucher campaigns"),
                Text("title", "Tiêu đề", "Ví voucher"),
                TextArea("description", "Mô tả", "Tập hợp voucher và campaign có thể tái sử dụng cho nhiều phân khúc khách hàng.")
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
            Section("booking", "consultationHero", "Hero tư vấn", 1, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Tư vấn chuyến đi theo cách của bạn"),
                TextArea("description", "Mô tả", "Chia sẻ nhu cầu để đội ngũ HV Travel tư vấn tour phù hợp với lịch trình và ngân sách của bạn.")
            }),
            Section("booking", "consultationBenefits", "Lợi ích tư vấn", 2, new List<ContentField>
            {
                Text("quickContactTitle", "Tiêu đề liên hệ nhanh", "Liên hệ nhanh"),
                Text("reasonsTitle", "Tiêu đề lý do chọn", "Tại sao chọn HV Travel?"),
                Text("formTitle", "Tiêu đề form", "Gửi yêu cầu tư vấn"),
                Text("submitText", "Nút gửi", "Gửi yêu cầu"),
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
                Text("submitText", "Nút gửi", "Tra cứu booking")
            }),
            Section("bookingLookup", "readyState", "Trạng thái sẵn sàng", 3, new List<ContentField>
            {
                Text("title", "Tiêu đề", "Sẵn sàng tra cứu"),
                TextArea("description", "Mô tả", "Sau khi nhập đúng mã booking và thông tin liên hệ, bạn sẽ thấy trạng thái đơn và mốc xử lý mới nhất ngay tại đây.")
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
