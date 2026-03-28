using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HVTravel.Web.Models;

public class AncillaryLeadRequestViewModel
{
    [Required(ErrorMessage = "Vui lòng chọn dịch vụ")]
    public string ServiceType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập điểm đến")]
    public string Destination { get; set; } = string.Empty;

    public DateTime? DepartureDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    [Range(1, 20, ErrorMessage = "Cần ít nhất 1 hành khách")]
    public int TravellersCount { get; set; } = 1;

    public string BudgetText { get; set; } = string.Empty;

    public string RequestNote { get; set; } = string.Empty;

    public IFormFile? Attachment { get; set; }
}

public class ServicesHubViewModel
{
    public AncillaryLeadRequestViewModel Request { get; set; } = new();
    public IReadOnlyList<ServiceCardViewModel> ServiceCards { get; set; } = Array.Empty<ServiceCardViewModel>();
}

public class ServiceCardViewModel
{
    public string ServiceType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AccentClass { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}
