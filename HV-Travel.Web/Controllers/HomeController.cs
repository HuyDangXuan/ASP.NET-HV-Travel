using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using HVTravel.Application.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRepository<Tour> _tourRepository;
    private readonly IRepository<ContactMessage> _contactMessageRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public HomeController(
        IRepository<Tour> tourRepository,
        IRepository<ContactMessage> contactMessageRepository,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _tourRepository = tourRepository;
        _contactMessageRepository = contactMessageRepository;
        _emailService = emailService;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Home";
        var tours = await _tourRepository.GetAllAsync();
        var featuredTours = tours
            .Where(t => IsPubliclyVisible(t.Status))
            .OrderByDescending(t => t.Rating)
            .Take(6)
            .ToList();
        return View(featuredTours);
    }

    private static bool IsPubliclyVisible(string? status)
    {
        return status is "Active" or "ComingSoon" or "SoldOut";
    }

    public IActionResult About()
    {
        ViewData["ActivePage"] = "About";
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        PrepareContactPage();
        return View(new ContactViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        PrepareContactPage();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var notificationEmail = ResolveNotificationEmail();
        if (string.IsNullOrWhiteSpace(notificationEmail))
        {
            ModelState.AddModelError(string.Empty, "Chưa cấu hình email nhận liên hệ. Vui lòng thử lại sau.");
            return View(model);
        }

        var contactMessage = new ContactMessage
        {
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Email = model.Email.Trim(),
            Subject = string.IsNullOrWhiteSpace(model.Subject) ? "Liên hệ chung" : model.Subject.Trim(),
            Message = model.Message.Trim(),
            NotificationEmail = notificationEmail,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            EmailSent = false,
            EmailError = string.Empty
        };

        await _contactMessageRepository.AddAsync(contactMessage);

        try
        {
            await _emailService.SendEmailAsync(
                notificationEmail,
                $"[HV Travel] Liên hệ mới - {contactMessage.Subject}",
                BuildContactEmailBody(contactMessage));

            contactMessage.EmailSent = true;
            contactMessage.EmailSentAt = DateTime.UtcNow;
            contactMessage.EmailError = string.Empty;
            contactMessage.UpdatedAt = DateTime.UtcNow;
            await _contactMessageRepository.UpdateAsync(contactMessage.Id, contactMessage);

            TempData["ContactSuccess"] = true;
            return RedirectToAction(nameof(Contact));
        }
        catch (Exception ex)
        {
            contactMessage.EmailSent = false;
            contactMessage.EmailError = ex.Message;
            contactMessage.UpdatedAt = DateTime.UtcNow;
            await _contactMessageRepository.UpdateAsync(contactMessage.Id, contactMessage);

            ModelState.AddModelError(string.Empty, "Chúng tôi đã lưu yêu cầu của bạn nhưng chưa gửi được email thông báo nội bộ. Vui lòng thử lại sau hoặc liên hệ hotline.");
            return View(model);
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private void PrepareContactPage()
    {
        ViewData["ActivePage"] = "Contact";
    }

    private string ResolveNotificationEmail()
    {
        return Environment.GetEnvironmentVariable("MAIL_TO")
            ?? _configuration["MAIL_TO"]
            ?? string.Empty;
    }

    private static string BuildContactEmailBody(ContactMessage contactMessage)
    {
        static string Encode(string value) => WebUtility.HtmlEncode(value);

        var builder = new StringBuilder();
        builder.Append("<div style=\"font-family:Arial,sans-serif;line-height:1.6;color:#0f172a\">");
        builder.Append("<h2 style=\"margin-bottom:16px\">Liên hệ mới từ website HV Travel</h2>");
        builder.Append("<table style=\"border-collapse:collapse;width:100%;max-width:720px\">");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0;width:180px\"><strong>Họ và tên</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0\">{Encode(contactMessage.FullName)}</td></tr>");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0\"><strong>Số điện thoại</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0\">{Encode(contactMessage.PhoneNumber)}</td></tr>");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0\"><strong>Email</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0\">{Encode(contactMessage.Email)}</td></tr>");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0\"><strong>Chủ đề</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0\">{Encode(contactMessage.Subject)}</td></tr>");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0\"><strong>Thời gian</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0\">{contactMessage.CreatedAt.ToLocalTime():HH:mm dd/MM/yyyy}</td></tr>");
        builder.Append($"<tr><td style=\"padding:8px;border:1px solid #e2e8f0;vertical-align:top\"><strong>Nội dung</strong></td><td style=\"padding:8px;border:1px solid #e2e8f0;white-space:pre-wrap\">{Encode(contactMessage.Message)}</td></tr>");
        builder.Append("</table>");
        builder.Append("</div>");
        return builder.ToString();
    }
}
