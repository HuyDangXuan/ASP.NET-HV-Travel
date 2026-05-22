using HVTravel.Application.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;

namespace HVTravel.Web.Services;

public class BookingWorkflowService
{
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Customer> _customerRepository;
    private readonly IRepository<Payment> _paymentRepository;
    private readonly IRepository<LoyaltyLedgerEntry> _ledgerRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly ISearchIndexingService? _searchIndexingService;

    public BookingWorkflowService(
        IRepository<Booking> bookingRepository,
        IRepository<Customer> customerRepository,
        IRepository<Payment> paymentRepository,
        IRepository<LoyaltyLedgerEntry> ledgerRepository,
        IRepository<Notification> notificationRepository,
        ISearchIndexingService? searchIndexingService = null)
    {
        _bookingRepository = bookingRepository;
        _customerRepository = customerRepository;
        _paymentRepository = paymentRepository;
        _ledgerRepository = ledgerRepository;
        _notificationRepository = notificationRepository;
        _searchIndexingService = searchIndexingService;
    }

    public async Task<bool> ProcessGatewayCallbackAsync(PaymentGatewayWebhookModel model)
    {
        var booking = (await _bookingRepository.FindAsync(b => b.BookingCode == model.BookingCode)).FirstOrDefault();
        if (booking == null)
        {
            return false;
        }

        booking.PaymentTransactions ??= new List<PaymentTransaction>();
        booking.Events ??= new List<BookingEvent>();
        booking.HistoryLog ??= new List<BookingHistoryLog>();

        var existingTransaction = booking.PaymentTransactions.FirstOrDefault(transaction =>
            string.Equals(transaction.TransactionId, model.TransactionId, StringComparison.OrdinalIgnoreCase));

        if (existingTransaction != null && string.Equals(existingTransaction.Status, "Success", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var isSuccess = string.Equals(model.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Status, "PAID", StringComparison.OrdinalIgnoreCase)
            || string.Equals(model.Status, "SUCCESSFUL", StringComparison.OrdinalIgnoreCase);

        if (existingTransaction == null)
        {
            existingTransaction = new PaymentTransaction
            {
                Provider = model.Provider,
                Method = string.IsNullOrWhiteSpace(model.Method) ? "OnlineGateway" : model.Method,
                TransactionId = model.TransactionId,
                Reference = model.Reference,
                Amount = model.Amount,
                Status = isSuccess ? "Success" : model.Status,
                ReceivedFromWebhook = true,
                PayloadHash = BuildPayloadHash(model),
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };
            booking.PaymentTransactions.Add(existingTransaction);
        }
        else
        {
            existingTransaction.Status = isSuccess ? "Success" : model.Status;
            existingTransaction.Reference = model.Reference;
            existingTransaction.Amount = model.Amount;
            existingTransaction.PayloadHash = BuildPayloadHash(model);
            existingTransaction.ProcessedAt = DateTime.UtcNow;
        }

        if (isSuccess)
        {
            booking.PaymentStatus = "Paid";
            booking.Status = "Confirmed";
            booking.ConfirmedAt ??= DateTime.UtcNow;
            booking.ReceiptNumber ??= model.Reference;
            booking.UpdatedAt = DateTime.UtcNow;

            AddBookingEvent(booking, "payment", "Thanh toán thành công", $"Giao dịch {model.TransactionId} đã được xác nhận.", model.Provider);

            await EnsurePaymentRecordAsync(booking, model);
            await AwardPointsOnceAsync(booking, model.TransactionId, model.Amount);
            await EnsureNotificationAsync(booking, model.TransactionId, model.Reference);
        }
        else
        {
            booking.PaymentStatus = "Failed";
            booking.UpdatedAt = DateTime.UtcNow;
            AddBookingEvent(booking, "payment", "Thanh toán thất bại", $"Gateway trả về trạng thái {model.Status}.", model.Provider);
        }

        await _bookingRepository.UpdateAsync(booking.Id, booking);
        await (_searchIndexingService?.UpsertBookingAsync(booking) ?? Task.CompletedTask);
        return true;
    }

    public static int CalculateEarnedPoints(decimal amount)
    {
        return (int)Math.Floor(amount / 20000m);
    }

    private async Task EnsurePaymentRecordAsync(Booking booking, PaymentGatewayWebhookModel model)
    {
        var existing = (await _paymentRepository.FindAsync(payment => payment.TransactionId == model.TransactionId)).FirstOrDefault();
        if (existing != null)
        {
            existing.Status = "Success";
            existing.Amount = model.Amount;
            existing.PaymentDate = DateTime.UtcNow;
            existing.PaymentMethod = string.IsNullOrWhiteSpace(model.Method) ? "OnlineGateway" : model.Method;
            await _paymentRepository.UpdateAsync(existing.Id, existing);
            return;
        }

        await _paymentRepository.AddAsync(new Payment
        {
            BookingId = booking.Id,
            Amount = model.Amount,
            PaymentMethod = string.IsNullOrWhiteSpace(model.Method) ? "OnlineGateway" : model.Method,
            TransactionId = model.TransactionId,
            PaymentDate = DateTime.UtcNow,
            Status = "Success"
        });
    }

    private async Task AwardPointsOnceAsync(Booking booking, string transactionId, decimal amount)
    {
        var existingLedger = (await _ledgerRepository.FindAsync(entry => entry.BookingId == booking.Id))
            .FirstOrDefault(entry => string.Equals(entry.Note, transactionId, StringComparison.OrdinalIgnoreCase));

        if (existingLedger != null)
        {
            return;
        }

        var customer = await _customerRepository.GetByIdAsync(booking.CustomerId);
        if (customer == null)
        {
            return;
        }

        customer.Stats ??= new CustomerStats();
        var points = CalculateEarnedPoints(amount);
        customer.Stats.LoyaltyPoints += points;
        customer.Stats.LifetimeSpend += amount;
        customer.Stats.TripCount += 1;
        customer.Stats.LastActivity = DateTime.UtcNow;
        customer.Stats.Tier = CustomerPortalService.ResolveTier(customer.Stats.LoyaltyPoints, customer.Stats.LifetimeSpend).Name;

        await _ledgerRepository.AddAsync(new LoyaltyLedgerEntry
        {
            CustomerId = customer.Id,
            BookingId = booking.Id,
            Type = "Earn",
            Title = $"Tích điểm cho booking {booking.BookingCode}",
            Points = points,
            BalanceAfter = customer.Stats.LoyaltyPoints,
            Note = transactionId,
            CreatedAt = DateTime.UtcNow
        });

        await _customerRepository.UpdateAsync(customer.Id, customer);
        await (_searchIndexingService?.UpsertCustomerAsync(customer) ?? Task.CompletedTask);
    }

    private async Task EnsureNotificationAsync(Booking booking, string transactionId, string reference)
    {
        var title = $"Thanh toán booking {booking.BookingCode} thành công";
        var existingNotification = (await _notificationRepository.FindAsync(notification => notification.RecipientId == booking.CustomerId && notification.Title == title)).FirstOrDefault();
        if (existingNotification != null)
        {
            return;
        }

        await _notificationRepository.AddAsync(new Notification
        {
            RecipientId = booking.CustomerId,
            Type = "Order",
            Title = title,
            Message = $"Giao dịch {transactionId} đã xác nhận. Mã tham chiếu: {reference}.",
            Link = $"/CustomerPortal?bookingCode={booking.BookingCode}",
            CreatedAt = DateTime.UtcNow
        });
    }

    private static void AddBookingEvent(Booking booking, string type, string title, string description, string actor)
    {
        booking.Events.Add(new BookingEvent
        {
            Type = type,
            Title = title,
            Description = description,
            Actor = actor,
            OccurredAt = DateTime.UtcNow,
            VisibleToCustomer = true
        });

        booking.HistoryLog.Add(new BookingHistoryLog
        {
            Action = title,
            Note = description,
            User = actor,
            Timestamp = DateTime.UtcNow
        });
    }

    private static string BuildPayloadHash(PaymentGatewayWebhookModel model)
    {
        return $"{model.BookingCode}|{model.TransactionId}|{model.Status}|{model.Amount:0.##}|{model.Reference}";
    }
}
