using HVTravel.Application.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class ServicesController : Controller
{
    private readonly IRepository<AncillaryLead> _leadRepository;
    private readonly IRepository<Notification> _notificationRepository;
    private readonly ISearchIndexingService? _searchIndexingService;

    public ServicesController(
        IRepository<AncillaryLead> leadRepository,
        IRepository<Notification> notificationRepository,
        ISearchIndexingService? searchIndexingService = null)
    {
        _leadRepository = leadRepository;
        _notificationRepository = notificationRepository;
        _searchIndexingService = searchIndexingService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        ViewData["Title"] = "Dịch vụ lẻ & báo giá nhanh";
        ViewData["ActivePage"] = "Services";
        return View(CreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestQuote(AncillaryLeadRequestViewModel model)
    {
        ViewData["Title"] = "Dịch vụ lẻ & báo giá nhanh";
        ViewData["ActivePage"] = "Services";

        if (!ModelState.IsValid)
        {
            var invalidModel = CreateViewModel();
            invalidModel.Request = model;
            return View("Index", invalidModel);
        }

        var lead = new AncillaryLead
        {
            ServiceType = model.ServiceType.Trim(),
            FullName = model.FullName.Trim(),
            Email = model.Email.Trim(),
            Phone = model.Phone.Trim(),
            Destination = model.Destination.Trim(),
            DepartureDate = model.DepartureDate,
            ReturnDate = model.ReturnDate,
            TravellersCount = model.TravellersCount,
            BudgetText = model.BudgetText?.Trim() ?? string.Empty,
            RequestNote = model.RequestNote?.Trim() ?? string.Empty,
            Status = "New",
            QuoteStatus = "Open",
            AssignedTo = "Sales queue",
            Source = "Website",
            SlaDueAt = DateTime.UtcNow.AddHours(8),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _leadRepository.AddAsync(lead);
        await (_searchIndexingService?.UpsertServiceLeadAsync(lead) ?? Task.CompletedTask);
        await _notificationRepository.AddAsync(new Notification
        {
            RecipientId = "ALL",
            Type = "Lead",
            Title = $"Lead {lead.ServiceType} mới",
            Message = $"{lead.FullName} cần báo giá {lead.ServiceType} cho {lead.Destination}.",
            Link = "/Admin/ServiceLeads",
            CreatedAt = DateTime.UtcNow
        });

        TempData["ServiceLeadSuccess"] = true;
        return RedirectToAction(nameof(Index));
    }

    private static ServicesHubViewModel CreateViewModel()
    {
        return new ServicesHubViewModel
        {
            ServiceCards = new List<ServiceCardViewModel>
            {
                new() { ServiceType = "Flight", Title = "Vé máy bay", Description = "Giữ chỗ nhanh, tối ưu lịch bay và baggage theo hành trình.", AccentClass = "from-sky-500 to-blue-700", Icon = "flight" },
                new() { ServiceType = "Hotel", Title = "Khách sạn", Description = "Từ city break đến resort gia đình với báo giá theo ngân sách.", AccentClass = "from-emerald-500 to-teal-700", Icon = "hotel" },
                new() { ServiceType = "Combo", Title = "Combo", Description = "Gom vé + phòng + tour lẻ để khóa deal trọn gói.", AccentClass = "from-amber-500 to-orange-700", Icon = "package_2" },
                new() { ServiceType = "Visa", Title = "Visa", Description = "Checklist hồ sơ, timeline nộp và hỗ trợ tỷ lệ đậu tốt hơn.", AccentClass = "from-rose-500 to-fuchsia-700", Icon = "assignment" }
            }
        };
    }
}
