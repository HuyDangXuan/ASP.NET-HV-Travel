using HVTravel.Domain.Entities;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class BookingJourneyPresenter
{
    public BookingJourneyPageVm BuildCreatePage(BookingViewModel model, Tour tour)
    {
        var departure = tour.ResolveDeparture(model.DepartureId, model.SelectedStartDate)
            ?? tour.EffectiveDepartures.OrderBy(item => item.StartDate).FirstOrDefault();
        var adultPrice = departure?.AdultPrice > 0m ? departure.AdultPrice : tour.Price?.Adult ?? 0m;
        var childPrice = departure?.ChildPrice > 0m ? departure.ChildPrice : tour.Price?.Child ?? 0m;
        var infantPrice = departure?.InfantPrice > 0m ? departure.InfantPrice : tour.Price?.Infant ?? 0m;
        var subtotal = (adultPrice * model.AdultCount) + (childPrice * model.ChildCount) + (infantPrice * model.InfantCount);
        var amountDueNow = string.Equals(model.PaymentPlanType, "Deposit", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(subtotal * 0.30m, 0, MidpointRounding.AwayFromZero)
            : subtotal;
        var travellers = Math.Max(1, model.AdultCount) + Math.Max(0, model.ChildCount) + Math.Max(0, model.InfantCount);

        return new BookingJourneyPageVm
        {
            StageKey = "build",
            Eyebrow = "Hành trình đặt tour",
            Title = string.IsNullOrWhiteSpace(tour.Name) ? "Giữ chỗ chuyến đi" : tour.Name,
            Description = "Chọn ngày đi, số khách và thông tin liên hệ trước khi sang bước thanh toán.",
            StageIndex = 1,
            ShowStageBar = true,
            Summary = new BookingSummaryVm
            {
                Eyebrow = "Tóm tắt giữ chỗ",
                Title = string.IsNullOrWhiteSpace(tour.Name) ? "Chuyến đi của bạn" : tour.Name,
                PrimaryAmount = FormatCurrency(amountDueNow),
                SecondaryAmount = $"{FormatCurrency(subtotal)} tổng tạm tính",
                Note = "Báo giá cập nhật theo thời gian thực khi bạn đổi lịch khởi hành, số khách hoặc mã ưu đãi.",
                Rows = new List<BookingSummaryRowVm>
                {
                    new() { Label = "Khởi hành", Value = departure?.StartDate.ToString("dd/MM/yyyy") ?? "Chưa chọn" },
                    new() { Label = "Số khách", Value = $"{travellers} khách" },
                    new() { Label = "Kế hoạch", Value = ResolvePlanTypeLabel(model.PaymentPlanType) },
                    new() { Label = "Điểm đến", Value = tour.Destination?.City ?? "Liên hệ tư vấn" },
                    new() { Label = "Thời lượng", Value = tour.Duration?.Text ?? "Liên hệ tư vấn" }
                }
            },
            Support = BuildSupport(supportPhone: string.Empty, supportEmail: string.Empty)
        };
    }

    public BookingJourneyPageVm BuildPaymentPage(Booking booking, Tour? tour, string supportPhone, string supportEmail)
    {
        return new BookingJourneyPageVm
        {
            StageKey = "payment",
            Eyebrow = "Thanh toán booking",
            Title = string.IsNullOrWhiteSpace(booking.BookingCode) ? "Chọn cách thanh toán" : $"Thanh toán booking {booking.BookingCode}",
            Description = "Chọn cách bạn muốn thanh toán, tiếp tục checkout online hoặc gửi minh chứng chuyển khoản.",
            StageIndex = 2,
            ShowStageBar = true,
            Status = BuildStatus(booking, BookingJourneyStage.Payment),
            Summary = BuildSummary(booking, tour),
            Timeline = BuildTimeline(booking),
            Support = BuildSupport(supportPhone, supportEmail),
            PaymentMethods = BuildPaymentMethods(booking)
        };
    }

    public BookingJourneyPageVm BuildStatusPage(Booking booking, Tour? tour, BookingJourneyStage stage, string supportPhone, string supportEmail)
    {
        return new BookingJourneyPageVm
        {
            StageKey = stage switch
            {
                BookingJourneyStage.Success => "success",
                BookingJourneyStage.Failed => "failed",
                BookingJourneyStage.Error => "error",
                _ => "status"
            },
            Eyebrow = stage == BookingJourneyStage.Failed ? "Khôi phục thanh toán" : "Theo dõi booking",
            Title = stage == BookingJourneyStage.Failed ? "Thanh toán cần được hoàn tất lại" : $"Theo dõi booking {booking.BookingCode}",
            Description = stage == BookingJourneyStage.Failed
                ? "Phiên thanh toán chưa hoàn tất. Bạn có thể thử lại, khôi phục checkout hoặc đổi sang phương thức khác."
                : "Theo dõi trạng thái, tiến trình xử lý và các hành động tiếp theo từ cùng một màn hình.",
            StageIndex = 3,
            ShowStageBar = true,
            Status = BuildStatus(booking, stage),
            Summary = BuildSummary(booking, tour),
            Timeline = BuildTimeline(booking),
            Support = BuildSupport(supportPhone, supportEmail)
        };
    }

    public BookingJourneyPageVm BuildLookupPage(Booking booking, string supportPhone, string supportEmail)
    {
        return new BookingJourneyPageVm
        {
            StageKey = "lookup",
            Eyebrow = "Quản lý booking",
            Title = string.IsNullOrWhiteSpace(booking.BookingCode) ? "Tra cứu booking" : $"Booking {booking.BookingCode}",
            Description = "Kiểm tra nhanh trạng thái booking, lịch khởi hành và các mốc vận hành mới nhất.",
            ShowStageBar = false,
            Status = BuildStatus(booking, BookingJourneyStage.Lookup),
            Summary = BuildSummary(booking, null),
            Timeline = BuildTimeline(booking),
            Support = BuildSupport(supportPhone, supportEmail)
        };
    }

    public BookingJourneyPageVm BuildConsultationPage(bool success, string supportPhone, string supportEmail)
    {
        return new BookingJourneyPageVm
        {
            StageKey = "consultation",
            Eyebrow = "Nhờ tư vấn",
            Title = success ? "Yêu cầu tư vấn đã được ghi nhận" : "Nhờ đội ngũ HV Travel hỗ trợ chốt chuyến đi",
            Description = success
                ? "Đội tư vấn sẽ phản hồi theo khung thời gian bạn đã để lại."
                : "Nếu bạn chưa sẵn sàng thanh toán, hãy gửi mô tả ngắn để được gợi ý hành trình phù hợp hơn.",
            ShowStageBar = false,
            Status = new BookingJourneyStatusVm
            {
                Variant = success ? "ConsultationSubmitted" : "ConsultationOpen",
                Tone = success ? "success" : "neutral",
                Eyebrow = success ? "Đã ghi nhận" : "Hỗ trợ theo yêu cầu",
                Title = success ? "Yêu cầu tư vấn đã gửi thành công" : "Gửi mô tả ngắn cho chuyên viên tư vấn",
                Description = success
                    ? "Bạn sẽ nhận phản hồi qua điện thoại hoặc email trong giờ làm việc tiếp theo."
                    : "Chia sẻ ngày đi, ngân sách và số khách để đội ngũ gợi ý phương án phù hợp hơn thay vì buộc phải chốt ngay."
            },
            Support = BuildSupport(supportPhone, supportEmail)
        };
    }

    public BookingJourneyPageVm BuildErrorPage(string supportPhone, string supportEmail)
    {
        return new BookingJourneyPageVm
        {
            StageKey = "error",
            Eyebrow = "Hệ thống gián đoạn",
            Title = "Không thể hoàn tất thao tác lúc này",
            Description = "Bạn có thể quay lại booking, thử lại sau hoặc liên hệ ngay để đội hỗ trợ tiếp tục xử lý thủ công.",
            ShowStageBar = false,
            Status = new BookingJourneyStatusVm
            {
                Variant = "SystemError",
                Tone = "danger",
                Eyebrow = "Lỗi hệ thống",
                Title = "Phiên thao tác chưa thể hoàn tất",
                Description = "Đã xảy ra lỗi hệ thống trong quá trình xử lý. Vui lòng thử lại hoặc liên hệ hỗ trợ."
            },
            Support = BuildSupport(supportPhone, supportEmail)
        };
    }

    public BookingJourneyStatusVm BuildStatus(Booking booking, BookingJourneyStage stage)
    {
        var variant = ResolveVariant(booking, stage);
        return variant switch
        {
            "PaidConfirmed" => new BookingJourneyStatusVm
            {
                Variant = variant,
                Tone = "success",
                Eyebrow = "Đã xác nhận",
                Title = "Thanh toán hoàn tất và booking đã được xác nhận",
                Description = "Hệ thống đã ghi nhận giao dịch. Bạn có thể theo dõi tiến trình và tiếp tục quản lý booking trong cổng khách hàng.",
                Reference = booking.BookingCode
            },
            "ReservedPendingTransferReview" => new BookingJourneyStatusVm
            {
                Variant = variant,
                Tone = "warning",
                Eyebrow = "Chờ đối soát",
                Title = "Đã nhận minh chứng chuyển khoản, đang chờ đối soát",
                Description = "Booking đang được giữ chỗ trong khi đội vận hành xác minh khoản chuyển tiền của bạn.",
                Reference = booking.BookingCode
            },
            "ReservedCashCollection" => new BookingJourneyStatusVm
            {
                Variant = variant,
                Tone = "neutral",
                Eyebrow = "Giữ chỗ thành công",
                Title = "Booking đã được giữ chỗ, thanh toán sau bằng tiền mặt",
                Description = "Bạn có thể thanh toán tại văn phòng hoặc theo hướng dẫn từ đội điều hành trước ngày khởi hành.",
                Reference = booking.BookingCode
            },
            "PaymentFailed" => new BookingJourneyStatusVm
            {
                Variant = variant,
                Tone = "danger",
                Eyebrow = "Cần thanh toán lại",
                Title = "Thanh toán online chưa hoàn tất",
                Description = "Bạn có thể thử lại ngay, khôi phục phiên checkout hiện tại hoặc đổi sang chuyển khoản hoặc tiền mặt.",
                Reference = booking.BookingCode
            },
            _ => new BookingJourneyStatusVm
            {
                Variant = variant,
                Tone = "neutral",
                Eyebrow = "Đang xử lý",
                Title = "Booking đang ở trạng thái chờ xử lý",
                Description = "Tiếp tục theo dõi tiến trình để biết các bước cập nhật tiếp theo.",
                Reference = booking.BookingCode
            }
        };
    }

    public string ResolveVariant(Booking booking, BookingJourneyStage stage)
    {
        if (stage == BookingJourneyStage.Failed)
        {
            return "PaymentFailed";
        }

        if (string.Equals(booking.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
        {
            return "PaidConfirmed";
        }

        if (string.Equals(booking.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(booking.TransferProofBase64))
        {
            return "ReservedPendingTransferReview";
        }

        if (string.Equals(booking.Status, "Confirmed", StringComparison.OrdinalIgnoreCase)
            && string.Equals(booking.PaymentStatus, "Unpaid", StringComparison.OrdinalIgnoreCase))
        {
            return "ReservedCashCollection";
        }

        return stage == BookingJourneyStage.Payment ? "AwaitingPayment" : "BookingInProgress";
    }

    private BookingSummaryVm BuildSummary(Booking booking, Tour? tour)
    {
        var departureDate = booking.TourSnapshot?.StartDate?.ToString("dd/MM/yyyy") ?? "Chưa xác định";
        var destination = tour?.Destination?.City ?? booking.TourSnapshot?.Name ?? "HV Travel";

        return new BookingSummaryVm
        {
            Eyebrow = "Tóm tắt booking",
            Title = string.IsNullOrWhiteSpace(booking.TourSnapshot?.Name) ? booking.BookingCode : booking.TourSnapshot.Name,
            PrimaryAmount = FormatCurrency(booking.PaymentPlan?.AmountDueNow > 0m ? booking.PaymentPlan.AmountDueNow : booking.TotalAmount),
            SecondaryAmount = $"{FormatCurrency(booking.PricingBreakdown?.GrandTotal ?? booking.TotalAmount)} tổng thanh toán",
            Note = "Bạn luôn có thể quay lại bước thanh toán để đổi phương thức hoặc tiếp tục phiên checkout hiện tại.",
            Rows = new List<BookingSummaryRowVm>
            {
                new() { Label = "Mã booking", Value = booking.BookingCode, Emphasized = true },
                new() { Label = "Khởi hành", Value = departureDate },
                new() { Label = "Trạng thái thanh toán", Value = ResolvePaymentStatusLabel(booking.PaymentStatus) },
                new() { Label = "Kế hoạch", Value = ResolvePlanTypeLabel(booking.PaymentPlan?.PlanType) },
                new() { Label = "Điểm đến", Value = destination },
                new() { Label = "Mã ưu đãi", Value = string.IsNullOrWhiteSpace(booking.CouponCode) ? "Không áp dụng" : booking.CouponCode },
                new() { Label = "Phiên checkout", Value = string.IsNullOrWhiteSpace(booking.CheckoutSessionId) ? "Không có" : booking.CheckoutSessionId }
            }
        };
    }

    private BookingTimelineVm BuildTimeline(Booking booking)
    {
        var items = (booking.Events ?? new List<BookingEvent>())
            .Where(item => item.VisibleToCustomer)
            .OrderByDescending(item => item.OccurredAt)
            .Select(item => new BookingTimelineItemVm
            {
                Title = item.Title,
                Description = item.Description,
                Meta = $"{item.Actor} · {item.OccurredAt:HH:mm dd/MM/yyyy}",
                Tone = item.Type
            })
            .ToList();

        return new BookingTimelineVm
        {
            Title = "Tiến trình xử lý",
            EmptyText = "Booking chưa có mốc xử lý hiển thị cho khách hàng.",
            Items = items
        };
    }

    private static BookingSupportVm BuildSupport(string supportPhone, string supportEmail)
    {
        return new BookingSupportVm
        {
            Title = "Cần người thật hỗ trợ?",
            Description = "Nếu bạn muốn chốt nhanh với tư vấn viên, hãy chuyển sang nhánh hỗ trợ thay vì rời khỏi hành trình.",
            Phone = supportPhone,
            Email = supportEmail,
            SecondaryNote = "Khung giờ hỗ trợ: 8:00 - 18:00, từ thứ Hai đến thứ Bảy."
        };
    }

    private static IReadOnlyList<BookingPaymentMethodVm> BuildPaymentMethods(Booking booking)
    {
        var selected = ResolveSelectedMethod(booking);
        return new List<BookingPaymentMethodVm>
        {
            new()
            {
                Key = "CreditCard",
                Label = "Thanh toán online",
                Description = "Tiếp tục phiên checkout hiện tại và xác nhận ngay trên hệ thống.",
                BodyTitle = "Ưu tiên khi bạn muốn chốt nhanh",
                BodyDescription = "Dùng lại checkout session hiện có để thanh toán ngay và nhận trạng thái xác nhận tức thời.",
                CTA = "Thanh toán online ngay",
                Accent = "primary",
                IsSelected = selected == "CreditCard"
            },
            new()
            {
                Key = "BankTransfer",
                Label = "Chuyển khoản ngân hàng",
                Description = "Giữ chỗ trước, gửi minh chứng để đội vận hành đối soát.",
                BodyTitle = "Phù hợp khi bạn cần thanh toán thủ công",
                BodyDescription = "Sau khi xác nhận phương thức này, booking sẽ chờ đối soát và mở phần tải minh chứng chuyển khoản.",
                CTA = "Tôi sẽ chuyển khoản",
                Accent = "warning",
                IsSelected = selected == "BankTransfer"
            },
            new()
            {
                Key = "Cash",
                Label = "Tiền mặt",
                Description = "Giữ chỗ trước và thanh toán sau tại văn phòng hoặc theo hướng dẫn điều hành.",
                BodyTitle = "Dành cho khách muốn giữ booking rồi xử lý thanh toán sau",
                BodyDescription = "Hệ thống sẽ xác nhận giữ chỗ và chuyển bạn sang trạng thái chờ thu tiền mặt.",
                CTA = "Giữ chỗ và thanh toán sau",
                Accent = "neutral",
                IsSelected = selected == "Cash"
            }
        };
    }

    private static string ResolveSelectedMethod(Booking booking)
    {
        var lastTransaction = (booking.PaymentTransactions ?? new List<PaymentTransaction>())
            .OrderByDescending(item => item.ProcessedAt ?? item.CreatedAt)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(lastTransaction?.Method))
        {
            return lastTransaction.Method;
        }

        if (string.Equals(booking.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(booking.TransferProofBase64))
        {
            return "BankTransfer";
        }

        return "CreditCard";
    }

    private static string ResolvePaymentStatusLabel(string? paymentStatus)
    {
        return paymentStatus?.Trim().ToLowerInvariant() switch
        {
            "paid" => "Đã thanh toán",
            "pending" => "Đang chờ xử lý",
            "unpaid" => "Chưa thanh toán",
            _ => string.IsNullOrWhiteSpace(paymentStatus) ? "Chưa cập nhật" : paymentStatus
        };
    }

    private static string ResolvePlanTypeLabel(string? planType)
    {
        return planType?.Trim().ToLowerInvariant() switch
        {
            "deposit" => "Đặt cọc 30%",
            "full" => "Thanh toán toàn bộ",
            _ => string.IsNullOrWhiteSpace(planType) ? "Thanh toán toàn bộ" : planType
        };
    }

    private static string FormatCurrency(decimal amount) => $"{amount:N0}₫";
}
