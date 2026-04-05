using System.ComponentModel.DataAnnotations;

namespace HVTravel.Web.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập email")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập nội dung")]
    public string Message { get; set; } = string.Empty;
}
