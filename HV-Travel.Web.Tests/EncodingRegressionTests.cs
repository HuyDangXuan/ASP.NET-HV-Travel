namespace HV_Travel.Web.Tests;

public class EncodingRegressionTests
{
    private static readonly string[] FilesToCheck =
    {
        @"HV-Travel.Web\Areas\Admin\Views\Content\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Payments\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Customers\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Shared\_Layout.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Details.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Edit.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Tours\Index.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Users\Create.cshtml",
        @"HV-Travel.Web\Areas\Admin\Views\Users\Edit.cshtml",
        @"HV-Travel.Web\Views\Home\Index.cshtml",
        @"HV-Travel.Web\Views\Home\About.cshtml",
        @"HV-Travel.Web\Views\Home\Contact.cshtml",
        @"HV-Travel.Web\Views\PublicTours\Index.cshtml",
        @"HV-Travel.Web\Views\PublicTours\Details.cshtml",
        @"HV-Travel.Web\Views\Destinations\Index.cshtml",
        @"HV-Travel.Web\Views\Promotions\Index.cshtml",
        @"HV-Travel.Web\Views\Services\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Index.cshtml",
        @"HV-Travel.Web\Views\Inspiration\Details.cshtml",
        @"HV-Travel.Web\Views\BookingLookup\Index.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Login.cshtml",
        @"HV-Travel.Web\Views\CustomerAuth\Register.cshtml",
        @"HV-Travel.Web\Views\CustomerPortal\Index.cshtml",
        @"HV-Travel.Web\Views\Booking\Consultation.cshtml",
        @"HV-Travel.Web\Views\Booking\Create.cshtml",
        @"HV-Travel.Web\Views\Booking\Payment.cshtml",
        @"HV-Travel.Web\Views\Booking\Success.cshtml",
        @"HV-Travel.Web\Views\Booking\Failed.cshtml",
        @"HV-Travel.Web\Views\Booking\Error.cshtml",
        @"HV-Travel.Web\Views\Shared\_LayoutPublic.cshtml",
        @"HV-Travel.Web\Views\Shared\_SupportChatWidget.cshtml",
        @"HV-Travel.Web\Services\PublicContentDefaults.cs",
        @"HV-Travel.Web\wwwroot\js\admin-cloudinary.js"
    };

    private static readonly string[] SuspiciousSequences =
    {
        "Ã",
        "Â",
        "Ä",
        "Æ",
        "áº",
        "á»",
        "â€™",
        "â€œ",
        "â€"
    };

    private static readonly Dictionary<string, string[]> ExpectedPhrasesByFile = new()
    {
        [@"HV-Travel.Web\Views\Home\Index.cshtml"] = new[]
        {
            "Trang Chủ",
            "Khám Phá",
            "Hành Trình Được Yêu Thích Nhất",
            "{tour.Duration?.Days}N{tour.Duration?.Nights}Đ"
        },
        [@"HV-Travel.Web\Views\Home\About.cshtml"] = new[]
        {
            "Câu chuyện",
            "Câu chuyện của chúng tôi",
            "Sứ mệnh"
        },
        [@"HV-Travel.Web\Views\Home\Contact.cshtml"] = new[]
        {
            "Liên Hệ",
            "Chúng tôi luôn sẵn sàng lắng nghe",
            "Gửi Tin Nhắn Cho Chúng Tôi"
        },
        [@"HV-Travel.Web\Views\PublicTours\Index.cshtml"] = new[]
        {
            "Tour tuyển chọn",
            "Tìm điểm đến, loại tour, mùa lễ hội...",
            "Không tìm thấy tour nào",
            "Tour đã lưu"
        },
        [@"HV-Travel.Web\Views\Destinations\Index.cshtml"] = new[]
        {
            "Trang này hỗ trợ marketing mở landing",
            "Điểm đến nổi bật",
            "Bộ sưu tập nổi bật",
            "Từ"
        },
        [@"HV-Travel.Web\Views\Promotions\Index.cshtml"] = new[]
        {
            "Trang mới gom toàn bộ khuyến mãi",
            "Ưu đãi theo chiến dịch",
            "Khuyến mãi tạo urgency",
            "Đơn tối thiểu"
        },
        [@"HV-Travel.Web\Views\Services\Index.cshtml"] = new[]
        {
            "Mô hình mới theo hướng ecosystem",
            "Hệ sinh thái dịch vụ",
            "Yêu cầu báo giá",
            "Gửi yêu cầu báo giá"
        },
        [@"HV-Travel.Web\Views\Inspiration\Index.cshtml"] = new[]
        {
            "Khu vực nội dung giúp đội marketing",
            "Cẩm nang hành trình",
            "Bài viết nổi bật",
            "Đọc bài viết"
        },
        [@"HV-Travel.Web\Views\Booking\Consultation.cshtml"] = new[]
        {
            "Tư Vấn Chuyến Đi Theo Cách Của Bạn",
            "Tại Sao Chọn HV Travel?",
            "Gửi Yêu Cầu Thành Công!"
        },
        [@"HV-Travel.Web\Views\BookingLookup\Index.cshtml"] = new[]
        {
            "Trang chủ",
            "Tra cứu booking",
            "Thông tin tra cứu",
            "Sẵn sàng tra cứu"
        },
        [@"HV-Travel.Web\Views\Booking\Success.cshtml"] = new[]
        {
            "Trang chủ",
            "Đặt Tour Thành Công!",
            "Thông Tin Đặt Tour",
            "Về Trang Chủ"
        },
        [@"HV-Travel.Web\Views\Booking\Failed.cshtml"] = new[]
        {
            "Trang chủ",
            "Thanh Toán Thất Bại",
            "Thử Lại",
            "Gọi Hỗ Trợ"
        },
        [@"HV-Travel.Web\Views\Booking\Error.cshtml"] = new[]
        {
            "Trang chủ",
            "Lỗi Hệ Thống",
            "Đã Xảy Ra Lỗi",
            "Tải Lại"
        },
        [@"HV-Travel.Web\Services\PublicContentDefaults.cs"] = new[]
        {
            "Tư vấn chuyến đi theo cách của bạn",
            "Tra cứu booking trong vài giây",
            "Đã xảy ra lỗi hệ thống trong quá trình xử lý. Vui lòng thử lại sau."
        }
    };

    [Theory]
    [MemberData(nameof(GetFiles))]
    public void File_Should_Not_Contain_Mojibake(string relativePath)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        foreach (var suspiciousSequence in SuspiciousSequences)
        {
            Assert.DoesNotContain(suspiciousSequence, content);
        }
    }

    [Theory]
    [MemberData(nameof(GetExpectedPhraseCases))]
    public void File_Should_Contain_Expected_Vietnamese_Phrase(string relativePath, string expectedPhrase)
    {
        var content = File.ReadAllText(GetRepoPath(relativePath));

        Assert.Contains(expectedPhrase, content);
    }

    public static IEnumerable<object[]> GetFiles()
    {
        return FilesToCheck.Select(static path => new object[] { path });
    }

    public static IEnumerable<object[]> GetExpectedPhraseCases()
    {
        foreach (var entry in ExpectedPhrasesByFile)
        {
            foreach (var phrase in entry.Value)
            {
                yield return new object[] { entry.Key, phrase };
            }
        }
    }

    private static string GetRepoPath(string relativePath)
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return Path.Combine(repoRoot, relativePath);
    }
}
