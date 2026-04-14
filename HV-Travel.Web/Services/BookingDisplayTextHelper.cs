namespace HVTravel.Web.Services;

public static class BookingDisplayTextHelper
{
    public static string BookingStatus(string? value)
    {
        var text = PublicTextSanitizer.NormalizeText(value);
        var normalizedStatus = text.ToLowerInvariant();
        return normalizedStatus switch
        {
            "pending" => "Chờ xử lý",
            "pendingpayment" => "Chờ thanh toán",
            "paid" => "Đã thanh toán",
            "confirmed" => "Đã xác nhận",
            "completed" => "Hoàn thành",
            "cancelled" => "Đã hủy",
            "refunded" => "Đã hoàn tiền",
            _ => string.IsNullOrWhiteSpace(text) ? "-" : text
        };
    }

    public static string NormalizeCmsText(string? value)
    {
        var text = PublicTextSanitizer.NormalizeText(value);
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return text switch
        {
            "Tour snapshot" => "Thông tin tour",
            "Departure" => "Đợt khởi hành",
            "Traveller" => "Hành khách",
            "Pricing controls" => "Tùy chọn thanh toán",
            "Coupon" => "Mã ưu đãi",
            "Payment plan" => "Hình thức thanh toán",
            "Full" => "Thanh toán toàn bộ",
            "Deposit" => "Đặt cọc 30%",
            "Quote" => "Báo giá",
            "Payment methods" => "Phương thức thanh toán",
            "Mock online gateway" => "Thanh toán online",
            "Cash hold" => "Giữ chỗ thanh toán tiền mặt",
            "Transfer proof" => "Minh chứng chuyển khoản",
            "Resume checkout" => "Khôi phục checkout",
            "Order summary" => "Tóm tắt đơn hàng",
            "Booking {0}" => "Đơn đặt {0}",
            "Checkout session" => "Phiên checkout",
            "Payment session" => "Phiên thanh toán",
            "Pending" => "Đang chờ xử lý",
            "Timeline" => "Tiến trình",
            "Failed state" => "Trạng thái thất bại",
            "Next actions" => "Bước tiếp theo",
            "Fulfillment" => "Xử lý dịch vụ",
            _ => text
        };
    }

    public static string PaymentPlan(string? value)
    {
        var text = PublicTextSanitizer.NormalizeText(value);
        return text switch
        {
            "Full" => "Thanh toán toàn bộ",
            "Deposit" => "Đặt cọc 30%",
            _ => string.IsNullOrWhiteSpace(text) ? "-" : text
        };
    }

    public static string PaymentStatus(string? value)
    {
        var text = PublicTextSanitizer.NormalizeText(value);
        return text switch
        {
            "Unpaid" => "Chưa thanh toán",
            "Pending" => "Đang chờ xử lý",
            "Partial" => "Thanh toán một phần",
            "Paid" => "Đã thanh toán",
            "Failed" => "Thanh toán thất bại",
            "Refunded" => "Đã hoàn tiền",
            _ => string.IsNullOrWhiteSpace(text) ? "-" : text
        };
    }

    public static string FulfillmentStatus(string? value)
    {
        var text = PublicTextSanitizer.NormalizeText(value);
        return text switch
        {
            "Reserved" => "Đã giữ chỗ",
            "PendingSupplier" => "Chờ nhà cung cấp xác nhận",
            "Pending" => "Đang xử lý",
            "Confirmed" => "Đã xác nhận",
            "Completed" => "Hoàn tất",
            "Cancelled" => "Đã hủy",
            _ => string.IsNullOrWhiteSpace(text) ? "-" : text
        };
    }
}
