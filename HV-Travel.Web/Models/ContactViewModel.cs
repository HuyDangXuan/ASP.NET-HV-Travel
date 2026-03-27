using System.ComponentModel.DataAnnotations;

namespace HVTravel.Web.Models;

public class ContactViewModel
{
    [Required(ErrorMessage = "Vui lòng nh?p h? và tên")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nh?p s? di?n tho?i")]
    [Phone(ErrorMessage = "S? di?n tho?i không h?p l?")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nh?p email")]
    [EmailAddress(ErrorMessage = "Email không h?p l?")]
    public string Email { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nh?p n?i dung")]
    public string Message { get; set; } = string.Empty;
}
