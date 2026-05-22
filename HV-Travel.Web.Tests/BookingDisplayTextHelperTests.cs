using HVTravel.Web.Services;

namespace HV_Travel.Web.Tests;

public class BookingDisplayTextHelperTests
{
    [Theory]
    [InlineData("Pending", "Chờ xử lý")]
    [InlineData("PendingPayment", "Chờ thanh toán")]
    [InlineData("pendingpayment", "Chờ thanh toán")]
    public void BookingStatus_ReturnsVietnameseLabel_ForKnownStatuses(string status, string expected)
    {
        var result = BookingDisplayTextHelper.BookingStatus(status);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void BookingStatus_ReturnsOriginalValue_ForUnknownStatus()
    {
        var result = BookingDisplayTextHelper.BookingStatus("CustomStatus");

        Assert.Equal("CustomStatus", result);
    }
}
