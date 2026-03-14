using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public static class PublicContentDefaults
{
    public static IReadOnlyList<ContentTabOption> Tabs => new List<ContentTabOption>
    {
        new() { Key = "site", Label = "Site-wide", Description = "Header, footer, brand, social, contact info" },
        new() { Key = "home", Label = "Home", Description = "Hero, stats, featured tours intro, commitments, CTA" },
        new() { Key = "about", Label = "About", Description = "Hero, story, mission/vision, team" },
        new() { Key = "contact", Label = "Contact", Description = "Hero, contact cards, form intro" },
        new() { Key = "publicTours", Label = "Tours", Description = "Hero, search placeholder, empty state" },
        new() { Key = "booking", Label = "Booking", Description = "Consultation hero, benefits, status copy" }
    };

    public static Dictionary<string, List<string>> Inventory => new()
    {
        ["site"] = new() { "header", "footerBrand", "footerExplore", "footerCompany", "contactInfo", "socialLinks", "seo" },
        ["home"] = new() { "hero", "stats", "featuredToursIntro", "commitments", "finalCta" },
        ["about"] = new() { "hero", "story", "missionVision", "team" },
        ["contact"] = new() { "hero", "cards", "formIntro" },
        ["publicTours"] = new() { "indexHero", "emptyState" },
        ["booking"] = new() { "consultationHero", "consultationBenefits", "statusCopy" }
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
                    Text("brandName", "Ten thuong hieu", "HV Travel"),
                    Text("brandTagline", "Tagline", "Explore Vietnam"),
                    Text("navHomeLabel", "Nhan Trang chu", "Trang Chu"),
                    Text("navToursLabel", "Nhan Tour", "Tour Du Lich"),
                    Text("navAboutLabel", "Nhan Gioi thieu", "Gioi Thieu"),
                    Text("navContactLabel", "Nhan Lien he", "Lien He"),
                    Text("registerLabel", "Nut dang ky", "Dang Ky"),
                    Text("loginLabel", "Nut dang nhap", "Dang Nhap")
                }),
                Group("footerBrand", "Footer brand", 2, new List<ContentField>
                {
                    Text("title", "Tieu de", "HV Travel"),
                    Text("tagline", "Tagline", "Explore Vietnam"),
                    TextArea("description", "Mo ta footer", "Kham pha ve dep Viet Nam voi nhung tour du lich duoc thiet ke rieng. Trai nghiem van hoa, am thuc va thien nhien tuyet voi.")
                }),
                Group("footerExplore", "Footer kham pha", 3, new List<ContentField>
                {
                    Text("title", "Tieu de khoi", "Kham Pha"),
                    Text("allToursLabel", "Link tat ca tour", "Tat Ca Tour"),
                    Text("popularToursLabel", "Link tour pho bien", "Tour Pho Bien"),
                    Text("hotDestinationsLabel", "Link diem den hot", "Diem Den Hot"),
                    Text("specialOffersLabel", "Link uu dai", "Uu Dai Dac Biet")
                }),
                Group("footerCompany", "Footer cong ty", 4, new List<ContentField>
                {
                    Text("title", "Tieu de khoi", "Cong Ty"),
                    Text("aboutLabel", "Link gioi thieu", "Gioi Thieu"),
                    Text("contactLabel", "Link lien he", "Lien He"),
                    Text("privacyLabel", "Link chinh sach", "Chinh Sach Bao Mat"),
                    Text("termsLabel", "Link dieu khoan", "Dieu Khoan Su Dung")
                }),
                Group("contactInfo", "Thong tin lien he", 5, new List<ContentField>
                {
                    Text("title", "Tieu de", "Lien He"),
                    Text("address", "Dia chi", "123 Duong Nguyen Hue, Quan 1, TP.HCM"),
                    Text("phoneNumber", "So dien thoai", "+84 (28) 3822 9999"),
                    Url("email", "Email", "info@hvtravel.vn"),
                    Text("businessHours", "Gio lam viec", "T2 - T7: 8:00 - 18:00")
                }),
                Group("socialLinks", "Mang xa hoi", 6, new List<ContentField>
                {
                    Url("facebookUrl", "Facebook URL", "#"),
                    Url("instagramUrl", "Instagram URL", "#"),
                    Url("youtubeUrl", "YouTube URL", "#")
                }),
                Group("seo", "SEO mac dinh", 7, new List<ContentField>
                {
                    TextArea("defaultMetaDescription", "Meta description mac dinh", "HV Travel - Kham pha Viet Nam cung nhung tour du lich tuyet voi nhat")
                })
            }
        };
    }

    public static List<ContentSection> CreateAllSections()
    {
        return new List<ContentSection>
        {
            Section("home", "hero", "Home hero", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Kham pha hon 0+ diem den tuyet voi"),
                Text("titleLine1", "Tieu de dong 1", "Kham Pha"),
                Text("titleHighlight", "Tieu de highlight", "Viet Nam"),
                Text("titleLine2", "Tieu de dong 2", "Theo Cach Cua Ban"),
                TextArea("description", "Mo ta", "Nhung hanh trinh duoc thiet ke rieng, trai nghiem van hoa doc dao va phong canh thien nhien tuyet dep dang cho don ban."),
                Text("primaryCtaText", "CTA chinh", "Kham Pha Ngay"),
                Text("secondaryCtaText", "CTA phu", "Lien He Tu Van")
            }),
            Section("home", "stats", "Home marketing stats", 2, new List<ContentField>
            {
                Text("stat1Number", "So lieu 1", "500+"),
                Text("stat1Label", "Nhan 1", "Tour Da To Chuc"),
                Text("stat2Number", "So lieu 2", "10K+"),
                Text("stat2Label", "Nhan 2", "Khach Hai Long"),
                Text("stat3Number", "So lieu 3", "50+"),
                Text("stat3Label", "Nhan 3", "Diem Den"),
                Text("stat4Number", "So lieu 4", "4.9*"),
                Text("stat4Label", "Nhan 4", "Danh Gia TB")
            }),
            Section("home", "featuredToursIntro", "Home featured tours intro", 3, new List<ContentField>
            {
                Text("badgeText", "Badge", "Tour Noi Bat"),
                Text("title", "Tieu de", "Hanh Trinh Duoc Yeu Thich Nhat"),
                TextArea("description", "Mo ta", "Nhung tour du lich hang dau duoc danh gia cao boi hang ngan du khach"),
                Text("viewAllText", "Nut xem tat ca", "Xem Tat Ca Tour")
            }),
            Section("home", "commitments", "Home commitments", 4, new List<ContentField>
            {
                Text("title", "Tieu de", "Cam Ket Chat Luong"),
                TextArea("description", "Mo ta", "Dich vu chuyen nghiep, lich trinh chin chu va trai nghiem dang nho trong tung hanh trinh"),
                Text("item1Title", "Cam ket 1", "An Toan Tuyet Doi"),
                TextArea("item1Description", "Mo ta 1", "Lich trinh duoc to chuc ky luong voi doi ngu ho tro giau kinh nghiem."),
                Text("item2Title", "Cam ket 2", "Trai Nghiem Premium"),
                TextArea("item2Description", "Mo ta 2", "Tour duoc thiet ke tinh gon nhung van giu chat luong dich vu cao."),
                Text("item3Title", "Cam ket 3", "Ho Tro 24/7"),
                TextArea("item3Description", "Mo ta 3", "Doi ngu cua chung toi luon san sang ho tro trong suot hanh trinh."),
                Text("item4Title", "Cam ket 4", "Gia Tot Nhat"),
                TextArea("item4Description", "Mo ta 4", "Muc gia minh bach, toi uu ngan sach va phu hop nhieu nhom khach.")
            }),
            Section("home", "finalCta", "Home final CTA", 5, new List<ContentField>
            {
                Text("title", "Tieu de", "San Sang Cho Hanh Trinh Tiep Theo?"),
                TextArea("description", "Mo ta", "Hay de HV Travel dong hanh cung ban trong chuyen di tiep theo voi lich trinh duoc thiet ke rieng."),
                Text("primaryCtaText", "CTA chinh", "Lien He Ngay"),
                Text("secondaryCtaText", "CTA phu", "Kham Pha Tour")
            }),
            Section("about", "hero", "About hero", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Ve Chung Toi"),
                Text("titleLine1", "Tieu de dong 1", "Cau Chuyen"),
                Text("titleHighlight", "Tieu de highlight", "HV Travel"),
                TextArea("description", "Mo ta", "Tim hieu hanh trinh phat trien va triet ly tao nen nhung chuyen di mang dau an rieng cua HV Travel.")
            }),
            Section("about", "story", "About story", 2, new List<ContentField>
            {
                Text("title", "Tieu de", "Hon 10 Nam Kinh Nghiem Trong Nganh Du Lich"),
                TextArea("description1", "Doan 1", "HV Travel duoc thanh lap voi niem dam me mang den nhung trai nghiem du lich tuyet voi nhat cho du khach."),
                TextArea("description2", "Doan 2", "Chung toi tin rang moi chuyen di khong chi la mot hanh trinh den noi moi, ma con la co hoi de kham pha ban than, ket noi voi van hoa va con nguoi.")
            }),
            Section("about", "missionVision", "Mission and vision", 3, new List<ContentField>
            {
                Text("title", "Tieu de section", "Su Menh & Tam Nhin"),
                Text("missionTitle", "Tieu de su menh", "Su Menh"),
                TextArea("missionText", "Noi dung su menh", "Mang den nhung hanh trinh duoc ca nhan hoa, an toan va giau cam xuc cho moi du khach."),
                Text("visionTitle", "Tieu de tam nhin", "Tam Nhin"),
                TextArea("visionText", "Noi dung tam nhin", "Tro thanh thuong hieu du lich duoc tin yeu hang dau cho nhung chuyen di kham pha Viet Nam."),
                Text("valuesTitle", "Tieu de gia tri", "Gia Tri Cot Loi"),
                TextArea("valuesText", "Noi dung gia tri", "Tan tam, minh bach, sang tao va luon lay trai nghiem khach hang lam trung tam.")
            }),
            Section("about", "team", "About team", 4, new List<ContentField>
            {
                Text("title", "Tieu de", "Nhung Con Nguoi Dang Sau HV Travel"),
                TextArea("description", "Mo ta", "Doi ngu tao nen cac hanh trinh chin chu va giau cam hung cho tung chuyen di."),
                Text("member1Name", "Ten thanh vien 1", "Nguyen Hoang Vu"),
                Text("member1Role", "Vai tro 1", "Nha Sang Lap"),
                Text("member2Name", "Ten thanh vien 2", "Le Minh Duc"),
                Text("member2Role", "Vai tro 2", "Truong Phong Tour"),
                Text("member3Name", "Ten thanh vien 3", "Tran Khanh Linh"),
                Text("member3Role", "Vai tro 3", "Cham Soc Khach Hang"),
                Text("member4Name", "Ten thanh vien 4", "Pham Gia Hung"),
                Text("member4Role", "Vai tro 4", "Dieu Hanh Tour")
            }),
            Section("contact", "hero", "Contact hero", 1, new List<ContentField>
            {
                Text("badgeText", "Badge", "Lien He"),
                Text("title", "Tieu de", "Chung Toi Luon San Sang Lang Nghe"),
                TextArea("description", "Mo ta", "Lien he voi HV Travel de duoc tu van hanh trinh phu hop va nhan ho tro nhanh chong.")
            }),
            Section("contact", "cards", "Contact cards", 2, new List<ContentField>
            {
                Text("addressTitle", "Tieu de dia chi", "Dia Chi"),
                Text("phoneTitle", "Tieu de dien thoai", "Dien Thoai"),
                Text("emailTitle", "Tieu de email", "Email"),
                Text("hoursTitle", "Tieu de gio lam viec", "Gio Lam Viec")
            }),
            Section("contact", "formIntro", "Contact form intro", 3, new List<ContentField>
            {
                Text("title", "Tieu de form", "Gui Tin Nhan Cho Chung Toi"),
                TextArea("description", "Mo ta form", "Dien thong tin cua ban va doi ngu HV Travel se phan hoi trong thoi gian som nhat."),
                Text("submitText", "Nut gui", "Gui Tin Nhan")
            }),
            Section("publicTours", "indexHero", "Tours hero", 1, new List<ContentField>
            {
                Text("titleLine1", "Tieu de dong 1", "Tour"),
                Text("titleHighlight", "Tieu de highlight", "Du Lich"),
                TextArea("description", "Mo ta", "Kham pha nhung hanh trinh noi bat khap Viet Nam voi lich trinh duoc tuyen chon ky luong."),
                Text("searchPlaceholder", "Placeholder tim kiem", "Tim diem den, tu khoa, thanh pho...")
            }),
            Section("publicTours", "emptyState", "Tours empty state", 2, new List<ContentField>
            {
                Text("title", "Tieu de", "Khong tim thay tour nao"),
                TextArea("description", "Mo ta", "Hay thu doi tu khoa hoac quay lai toan bo danh sach tour dang mo ban."),
                Text("ctaText", "Nut CTA", "Xem tat ca tour")
            }),
            Section("booking", "consultationHero", "Consultation hero", 1, new List<ContentField>
            {
                Text("title", "Tieu de", "Tu Van Chuyen Di Theo Cach Cua Ban"),
                TextArea("description", "Mo ta", "Chia se nhu cau de doi ngu HV Travel tu van tour phu hop voi lich trinh va ngan sach cua ban.")
            }),
            Section("booking", "consultationBenefits", "Consultation benefits", 2, new List<ContentField>
            {
                Text("quickContactTitle", "Tieu de lien he nhanh", "Lien He Nhanh"),
                Text("reasonsTitle", "Tieu de ly do chon", "Tai Sao Chon HV Travel?"),
                Text("formTitle", "Tieu de form", "Gui Yeu Cau Tu Van"),
                Text("submitText", "Nut gui", "Gui Yeu Cau"),
                Text("successTitle", "Tieu de thanh cong", "Gui Yeu Cau Thanh Cong!"),
                TextArea("successDescription", "Mo ta thanh cong", "Chung toi da nhan duoc thong tin va se lien he voi ban trong thoi gian som nhat."),
                Text("exploreToursText", "Nut kham pha tour", "Kham Pha Tour")
            }),
            Section("booking", "statusCopy", "Booking status copy", 3, new List<ContentField>
            {
                Text("successTitle", "Tieu de thanh cong", "Dat Tour Thanh Cong!"),
                TextArea("successDescription", "Mo ta thanh cong", "Cam on ban da dat tour tai HV Travel. Chung toi se lien he xac nhan trong thoi gian som nhat."),
                Text("failedTitle", "Tieu de that bai", "Thanh Toan That Bai"),
                TextArea("failedDescription", "Mo ta that bai", "Thanh toan khong thanh cong. Vui long thu lai hoac chon phuong thuc khac."),
                Text("errorTitle", "Tieu de loi", "Da Xay Ra Loi"),
                TextArea("errorDescription", "Mo ta loi", "Da xay ra loi he thong trong qua trinh xu ly. Vui long thu lai sau.")
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
