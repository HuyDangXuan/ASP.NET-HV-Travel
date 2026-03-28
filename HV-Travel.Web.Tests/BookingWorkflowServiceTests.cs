using HVTravel.Domain.Entities;
using HVTravel.Web.Services;
using HVTravel.Web.Models;
using HV_Travel.Web.Tests.TestSupport;

namespace HV_Travel.Web.Tests;

public class BookingWorkflowServiceTests
{
    [Fact]
    public async Task ProcessGatewayCallbackAsync_IsIdempotentAndAwardsPointsOnce()
    {
        var customer = new Customer
        {
            Id = "customer-1",
            FullName = "Lê Thanh",
            Email = "thanh@example.com",
            PhoneNumber = "0911222333",
            Stats = new CustomerStats
            {
                LoyaltyPoints = 100,
                LifetimeSpend = 0,
                TripCount = 0,
                ReferralCode = "LETHANH"
            }
        };

        var booking = new Booking
        {
            Id = "booking-1",
            BookingCode = "HV20260328001",
            CustomerId = customer.Id,
            TourId = "tour-1",
            TourSnapshot = new TourSnapshot { Name = "Singapore Fun", Code = "SG-01", Duration = "4 ngày 3 đêm", StartDate = DateTime.UtcNow.AddDays(20) },
            ContactInfo = new ContactInfo { Name = customer.FullName, Email = customer.Email, Phone = customer.PhoneNumber },
            TotalAmount = 5200000,
            Status = "Pending",
            PaymentStatus = "Pending",
            ParticipantsCount = 2
        };

        var bookingRepository = new InMemoryRepository<Booking>([booking]);
        var customerRepository = new InMemoryRepository<Customer>([customer]);
        var paymentRepository = new InMemoryRepository<Payment>();
        var ledgerRepository = new InMemoryRepository<LoyaltyLedgerEntry>();
        var notificationRepository = new InMemoryRepository<Notification>();

        var service = new BookingWorkflowService(
            bookingRepository,
            customerRepository,
            paymentRepository,
            ledgerRepository,
            notificationRepository);

        var webhook = new PaymentGatewayWebhookModel
        {
            BookingCode = booking.BookingCode,
            TransactionId = "txn-001",
            Provider = "HVPay",
            Status = "SUCCESS",
            Amount = booking.TotalAmount,
            Reference = "HVPAY-REF-001",
            Signature = "noop"
        };

        await service.ProcessGatewayCallbackAsync(webhook);
        await service.ProcessGatewayCallbackAsync(webhook);

        var storedBooking = await bookingRepository.GetByIdAsync(booking.Id);
        Assert.Equal("Confirmed", storedBooking.Status);
        Assert.Equal("Paid", storedBooking.PaymentStatus);
        Assert.Single(storedBooking.PaymentTransactions.Where(t => t.TransactionId == "txn-001"));

        var storedCustomer = await customerRepository.GetByIdAsync(customer.Id);
        Assert.Equal(360, storedCustomer.Stats.LoyaltyPoints);
        Assert.Equal(5200000, storedCustomer.Stats.LifetimeSpend);
        Assert.Equal(1, storedCustomer.Stats.TripCount);

        var payments = await paymentRepository.GetAllAsync();
        Assert.Single(payments);

        var ledgerEntries = await ledgerRepository.GetAllAsync();
        Assert.Single(ledgerEntries);

        var notifications = await notificationRepository.GetAllAsync();
        Assert.Single(notifications);
    }
}


