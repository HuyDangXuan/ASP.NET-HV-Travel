using HVTravel.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HVTravel.Web.Models;

public class BookingViewModel
{
    // Tour Info (display only — excluded from model binding)
    [BindNever]
    public Tour? Tour { get; set; }
    public string TourId { get; set; } = string.Empty;
    public DateTime? SelectedStartDate { get; set; }

    // Contact Info
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string ContactPhone { get; set; } = string.Empty;

    // Passenger counts
    [Range(1, 50, ErrorMessage = "Cần ít nhất 1 người lớn")]
    public int AdultCount { get; set; } = 1;

    [Range(0, 50)]
    public int ChildCount { get; set; } = 0;

    [Range(0, 50)]
    public int InfantCount { get; set; } = 0;

    // Extras
    public string SpecialRequests { get; set; } = string.Empty;

    // Payment
    public string SelectedPaymentMethod { get; set; } = string.Empty; // BankTransfer, CreditCard, Cash

    // Calculated (server-side)
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
    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string Phone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    public string TourInterest { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    public string Message { get; set; } = string.Empty;

    public string PreferredContactTime { get; set; } = string.Empty;
}
