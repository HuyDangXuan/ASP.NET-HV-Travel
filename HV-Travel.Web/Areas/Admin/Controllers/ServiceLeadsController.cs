using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme, Roles = "Admin,Manager,Staff")]
public class ServiceLeadsController : Controller
{
    private readonly IRepository<AncillaryLead> _leadRepository;

    public ServiceLeadsController(IRepository<AncillaryLead> leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<IActionResult> Index(string status = "", string serviceType = "", string search = "")
    {
        ViewData["AdminSection"] = "serviceleads";
        ViewData["Title"] = "Service leads";

        var leads = (await _leadRepository.GetAllAsync()).AsEnumerable();
        if (!string.IsNullOrWhiteSpace(status))
        {
            leads = leads.Where(lead => string.Equals(lead.Status, status, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(serviceType))
        {
            leads = leads.Where(lead => string.Equals(lead.ServiceType, serviceType, StringComparison.OrdinalIgnoreCase));
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            leads = leads.Where(lead =>
                lead.FullName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                lead.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                lead.Destination.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return View(leads.OrderByDescending(lead => lead.CreatedAt).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStatus(string id, string status, string assignedTo)
    {
        var lead = await _leadRepository.GetByIdAsync(id);
        if (lead == null)
        {
            return RedirectToAction(nameof(Index));
        }

        lead.Status = string.IsNullOrWhiteSpace(status) ? lead.Status : status.Trim();
        lead.AssignedTo = string.IsNullOrWhiteSpace(assignedTo) ? lead.AssignedTo : assignedTo.Trim();
        lead.UpdatedAt = DateTime.UtcNow;
        await _leadRepository.UpdateAsync(lead.Id, lead);
        return RedirectToAction(nameof(Index));
    }
}
