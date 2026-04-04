using HVTravel.Domain.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace HVTravel.Web.Models;

public class BookingViewModel
{
    [BindNever]
    public Tour? Tour { get; set; }
    public string TourId { get; set; } = string.Empty;
    public string DepartureId { get; set; } = string.Empty;
    public DateTime? SelectedStartDate { get; set; }

    [Required(ErrorMessage = "Vui l?ng nh?p h? t?n")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p email")]
    [EmailAddress(ErrorMessage = "Email kh?ng h?p l?")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p s? ?i?n tho?i")]
    [Phone(ErrorMessage = "S? ?i?n tho?i kh?ng h?p l?")]
    public string ContactPhone { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "C?n ?t nh?t 1 ng??i l?n")]
    public int AdultCount { get; set; } = 1;

    [Range(0, 50)]
    public int ChildCount { get; set; }

    [Range(0, 50)]
    public int InfantCount { get; set; }

    public string CouponCode { get; set; } = string.Empty;
    public string PaymentPlanType { get; set; } = "Full";
    public string SpecialRequests { get; set; } = string.Empty;
    public string SelectedPaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalParticipants => AdultCount + ChildCount + InfantCount;
}

public class BookingResultViewModel
{
    public Booking Booking { get; set; } = new();
    public Tour? Tour { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ConsultationViewModel
{
    [Required(ErrorMessage = "Vui l?ng nh?p h? t?n")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p s? ?i?n tho?i")]
    [Phone(ErrorMessage = "S? ?i?n tho?i kh?ng h?p l?")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email kh?ng h?p l?")]
    public string Email { get; set; } = string.Empty;

    public string TourInterest { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui l?ng nh?p n?i dung")]
    public string Message { get; set; } = string.Empty;

    public string PreferredContactTime { get; set; } = string.Empty;
}
