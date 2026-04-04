using HVTravel.Domain.Entities;
using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class PublicTextSanitizerTests
{
    [Fact]
    public void NormalizeArticleForDisplay_RepairsDoubleEncodedPublicContent()
    {
        var article = new TravelArticle
        {
            Title = ToMojibake("Cẩm nang Nhật Bản"),
            Summary = ToMojibake("Mẹo visa, mùa lá đỏ và lịch trình tự túc."),
            Body = ToMojibake("<p>Ăn gì ở Osaka và di chuyển như thế nào.</p>"),
            Category = ToMojibake("Cẩm nang"),
            Destination = ToMojibake("Nhật Bản"),
            Tags = [ToMojibake("visa"), ToMojibake("mùa lá đỏ")]
        };

        var result = PublicTextSanitizer.NormalizeArticleForDisplay(article);

        Assert.Equal("Cẩm nang Nhật Bản", result.Title);
        Assert.Equal("Mẹo visa, mùa lá đỏ và lịch trình tự túc.", result.Summary);
        Assert.Equal("<p>Ăn gì ở Osaka và di chuyển như thế nào.</p>", result.Body);
        Assert.Equal("Cẩm nang", result.Category);
        Assert.Equal("Nhật Bản", result.Destination);
        Assert.Equal(new[] { "visa", "mùa lá đỏ" }, result.Tags);
    }

    [Fact]
    public void NormalizeTourForDisplay_RepairsCommerceFieldsUsedByDetailAndCheckout()
    {
        var tour = new Tour
        {
            Name = ToMojibake("Tokyo mùa hoa anh đào"),
            Description = ToMojibake("Khám phá hành trình Nhật Bản trọn vẹn."),
            ShortDescription = ToMojibake("Tour mùa xuân nổi bật."),
            Destination = new Destination
            {
                City = ToMojibake("Tokyo"),
                Country = ToMojibake("Nhật Bản"),
                Region = ToMojibake("Đông Bắc Á")
            },
            Duration = new TourDuration
            {
                Text = ToMojibake("5 ngày 4 đêm")
            },
            ConfirmationType = ToMojibake("Xác nhận tức thì"),
            MeetingPoint = ToMojibake("Sân bay Tân Sơn Nhất"),
            Highlights = [ToMojibake("Ngắm hoa anh đào"), ToMojibake("Tắm onsen")],
            GeneratedInclusions = [ToMojibake("Khách sạn 4 sao")],
            GeneratedExclusions = [ToMojibake("Chi tiêu cá nhân")],
            Schedule =
            [
                new ScheduleItem
                {
                    Title = ToMojibake("Ngày 1: Bay đến Tokyo"),
                    Description = ToMojibake("Tập trung tại sân bay và làm thủ tục."),
                    Activities = [ToMojibake("Check-in"), ToMojibake("Ăn tối")]
                }
            ],
            CancellationPolicy = new TourCancellationPolicy
            {
                Summary = ToMojibake("Miễn phí hủy trước 48 giờ")
            },
            Seo = new SeoMetadata
            {
                Title = ToMojibake("Tour Tokyo mùa hoa anh đào"),
                Description = ToMojibake("Trang chi tiết tour Nhật Bản")
            },
            Departures =
            [
                new TourDeparture
                {
                    ConfirmationType = ToMojibake("Xác nhận tức thì")
                }
            ]
        };

        var result = PublicTextSanitizer.NormalizeTourForDisplay(tour);

        Assert.Equal("Tokyo mùa hoa anh đào", result.Name);
        Assert.Equal("Khám phá hành trình Nhật Bản trọn vẹn.", result.Description);
        Assert.Equal("Tour mùa xuân nổi bật.", result.ShortDescription);
        Assert.Equal("Nhật Bản", result.Destination.Country);
        Assert.Equal("Đông Bắc Á", result.Destination.Region);
        Assert.Equal("5 ngày 4 đêm", result.Duration.Text);
        Assert.Equal("Xác nhận tức thì", result.ConfirmationType);
        Assert.Equal("Sân bay Tân Sơn Nhất", result.MeetingPoint);
        Assert.Equal(new[] { "Ngắm hoa anh đào", "Tắm onsen" }, result.Highlights);
        Assert.Equal("Khách sạn 4 sao", result.GeneratedInclusions.Single());
        Assert.Equal("Chi tiêu cá nhân", result.GeneratedExclusions.Single());
        Assert.Equal("Ngày 1: Bay đến Tokyo", result.Schedule.Single().Title);
        Assert.Equal("Tập trung tại sân bay và làm thủ tục.", result.Schedule.Single().Description);
        Assert.Equal(new[] { "Check-in", "Ăn tối" }, result.Schedule.Single().Activities);
        Assert.Equal("Miễn phí hủy trước 48 giờ", result.CancellationPolicy.Summary);
        Assert.Equal("Tour Tokyo mùa hoa anh đào", result.Seo.Title);
        Assert.Equal("Trang chi tiết tour Nhật Bản", result.Seo.Description);
        Assert.Equal("Xác nhận tức thì", result.Departures.Single().ConfirmationType);
    }

    [Fact]
    public void NormalizeBookingForDisplay_RepairsSnapshotAndTimelineContent()
    {
        var booking = new Booking
        {
            TourSnapshot = new TourSnapshot
            {
                Name = ToMojibake("Tour Kyoto cổ kính"),
                Duration = ToMojibake("4 ngày 3 đêm")
            },
            ContactInfo = new ContactInfo
            {
                Name = ToMojibake("Nguyễn Lan")
            },
            Events =
            [
                new BookingEvent
                {
                    Title = ToMojibake("Đã tạo booking"),
                    Description = ToMojibake("Booking đang chờ thanh toán.")
                }
            ],
            HistoryLog =
            [
                new BookingHistoryLog
                {
                    Action = ToMojibake("Đã tạo booking"),
                    Note = ToMojibake("Chờ khách hoàn tất thanh toán."),
                    User = ToMojibake("Khách")
                }
            ]
        };

        var result = PublicTextSanitizer.NormalizeBookingForDisplay(booking);

        Assert.Equal("Tour Kyoto cổ kính", result.TourSnapshot.Name);
        Assert.Equal("4 ngày 3 đêm", result.TourSnapshot.Duration);
        Assert.Equal("Nguyễn Lan", result.ContactInfo.Name);
        Assert.Equal("Đã tạo booking", result.Events.Single().Title);
        Assert.Equal("Booking đang chờ thanh toán.", result.Events.Single().Description);
        Assert.Equal("Đã tạo booking", result.HistoryLog.Single().Action);
        Assert.Equal("Chờ khách hoàn tất thanh toán.", result.HistoryLog.Single().Note);
        Assert.Equal("Khách", result.HistoryLog.Single().User);
    }

    private static string ToMojibake(string value)
    {
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        return System.Text.Encoding.GetEncoding(1252).GetString(bytes);
    }
}