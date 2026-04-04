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

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string ContactName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string ContactPhone { get; set; } = string.Empty;

    [Range(1, 50, ErrorMessage = "Cần ít nhất 1 người lớn")]
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
