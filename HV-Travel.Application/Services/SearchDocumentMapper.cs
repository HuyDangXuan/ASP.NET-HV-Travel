using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;

namespace HVTravel.Application.Services;

internal static partial class SearchDocumentMapper
{
    [GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex WhitespaceRegex();

    public static BookingSearchDocument MapBooking(Booking booking)
    {
        return new BookingSearchDocument
        {
            Id = booking.Id ?? string.Empty,
            BookingCode = booking.BookingCode?.Trim() ?? string.Empty,
            ContactName = NormalizeText(booking.ContactInfo?.Name),
            ContactEmail = NormalizeText(booking.ContactInfo?.Email),
            ContactPhone = NormalizeText(booking.ContactInfo?.Phone),
            ContactPhoneNormalized = MeilisearchQueryHelpers.NormalizePhone(booking.ContactInfo?.Phone),
            TourName = NormalizeText(booking.TourSnapshot?.Name),
            BookingStatus = booking.Status?.Trim() ?? string.Empty,
            PaymentStatus = booking.PaymentStatus?.Trim() ?? string.Empty,
            CreatedAt = booking.CreatedAt,
            DepartureDate = booking.TourSnapshot?.StartDate,
            TotalAmount = booking.TotalAmount,
            IsDeleted = booking.IsDeleted,
            PublicLookupEnabled = booking.PublicLookupEnabled
        };
    }

    public static UserSearchDocument MapUser(User user)
    {
        return new UserSearchDocument
        {
            Id = user.Id ?? string.Empty,
            FullName = NormalizeText(user.FullName),
            Email = NormalizeText(user.Email),
            Role = user.Role?.Trim() ?? string.Empty,
            Status = user.Status?.Trim() ?? string.Empty,
            LastLogin = user.LastLogin,
            CreatedAt = user.CreatedAt
        };
    }

    public static ReviewSearchDocument MapReview(Review review, string? tourName, string? customerEmail)
    {
        return new ReviewSearchDocument
        {
            Id = review.Id ?? string.Empty,
            TourId = review.TourId ?? string.Empty,
            TourName = NormalizeText(tourName),
            CustomerId = review.CustomerId ?? string.Empty,
            CustomerEmail = NormalizeText(customerEmail),
            BookingId = review.BookingId ?? string.Empty,
            DisplayName = NormalizeText(review.DisplayName),
            Comment = NormalizeText(review.Comment),
            ModeratorName = NormalizeText(review.ModeratorName),
            ModerationStatus = review.ModerationStatus?.Trim() ?? "Pending",
            ModerationStatusRank = GetModerationStatusRank(review.ModerationStatus),
            IsVerifiedBooking = review.IsVerifiedBooking,
            CreatedAt = review.CreatedAt,
            Rating = review.Rating
        };
    }

    public static ServiceLeadSearchDocument MapServiceLead(AncillaryLead lead)
    {
        return new ServiceLeadSearchDocument
        {
            Id = lead.Id ?? string.Empty,
            FullName = NormalizeText(lead.FullName),
            Email = NormalizeText(lead.Email),
            PhoneNumber = NormalizeText(lead.Phone),
            Destination = NormalizeText(lead.Destination),
            ServiceType = lead.ServiceType?.Trim() ?? string.Empty,
            Status = lead.Status?.Trim() ?? string.Empty,
            CreatedAt = lead.CreatedAt
        };
    }

    public static CustomerSearchDocument MapCustomer(Customer customer, CustomerSearchAggregate aggregate)
    {
        return new CustomerSearchDocument
        {
            Id = customer.Id ?? string.Empty,
            CustomerCode = customer.CustomerCode?.Trim() ?? string.Empty,
            FullName = NormalizeText(customer.FullName),
            Email = NormalizeText(customer.Email),
            PhoneNumber = NormalizeText(customer.PhoneNumber),
            Segment = customer.Segment?.Trim() ?? string.Empty,
            Status = customer.Status?.Trim() ?? string.Empty,
            TotalOrders = aggregate.TotalOrders,
            TotalSpending = aggregate.TotalSpending,
            LastBookingDate = aggregate.LastBookingDate,
            CreatedAt = customer.CreatedAt
        };
    }

    public static PaymentAdminSearchDocument MapPaymentAdmin(Booking booking, string? customerName)
    {
        return new PaymentAdminSearchDocument
        {
            Id = booking.Id ?? string.Empty,
            BookingCode = booking.BookingCode?.Trim() ?? string.Empty,
            CustomerId = booking.CustomerId ?? string.Empty,
            CustomerName = NormalizeText(customerName),
            PaymentStatus = booking.PaymentStatus?.Trim() ?? string.Empty,
            BookingStatus = booking.Status?.Trim() ?? string.Empty,
            Amount = booking.TotalAmount,
            CreatedAt = booking.BookingDate == default ? booking.CreatedAt : booking.BookingDate,
            PaidAt = ResolvePaidAt(booking)
        };
    }

    public static CustomerSearchAggregate BuildCustomerAggregate(IEnumerable<Booking> bookings)
    {
        var bookingList = bookings.ToList();
        return new CustomerSearchAggregate
        {
            TotalOrders = bookingList.Count,
            TotalSpending = bookingList
                .Where(static booking => !string.Equals(booking.Status, "Cancelled", StringComparison.OrdinalIgnoreCase))
                .Where(static booking => !string.Equals(booking.Status, "Refunded", StringComparison.OrdinalIgnoreCase))
                .Sum(static booking => booking.TotalAmount),
            LastBookingDate = bookingList
                .Select(static booking => (DateTime?)booking.CreatedAt)
                .OrderByDescending(static bookingDate => bookingDate)
                .FirstOrDefault()
        };
    }

    private static int GetModerationStatusRank(string? moderationStatus)
    {
        return moderationStatus?.ToLowerInvariant() switch
        {
            "pending" => 0,
            "approved" => 1,
            "rejected" => 2,
            _ => 99
        };
    }

    private static DateTime? ResolvePaidAt(Booking booking)
    {
        return booking.PaymentTransactions?
            .Where(transaction => string.Equals(transaction.Status, "Success", StringComparison.OrdinalIgnoreCase))
            .Select(transaction => transaction.ProcessedAt ?? transaction.CreatedAt)
            .OrderByDescending(static value => value)
            .FirstOrDefault();
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var withoutTags = HtmlTagRegex().Replace(value, " ");
        var normalized = withoutTags.Replace('\u00A0', ' ');
        return WhitespaceRegex().Replace(normalized, " ").Trim();
    }
}

internal sealed class CustomerSearchAggregate
{
    public int TotalOrders { get; init; }

    public decimal TotalSpending { get; init; }

    public DateTime? LastBookingDate { get; init; }
}
