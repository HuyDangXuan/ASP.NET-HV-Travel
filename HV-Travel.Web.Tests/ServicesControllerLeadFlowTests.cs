using HVTravel.Domain.Entities;
using HVTravel.Web.Controllers;
using HVTravel.Web.Models;
using HV_Travel.Web.Tests.TestSupport;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace HV_Travel.Web.Tests;

public class ServicesControllerLeadFlowTests
{
    [Fact]
    public async Task RequestQuote_ValidForm_PersistsLeadAndCreatesNotification()
    {
        var leadRepository = new InMemoryRepository<AncillaryLead>();
        var notificationRepository = new InMemoryRepository<Notification>();
        var controller = new ServicesController(leadRepository, notificationRepository)
        {
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        var model = new AncillaryLeadRequestViewModel
        {
            ServiceType = "Visa",
            FullName = "Phạm Thảo",
            Email = "thao@example.com",
            Phone = "0908123123",
            Destination = "Nhật Bản",
            DepartureDate = DateTime.UtcNow.AddDays(45),
            ReturnDate = DateTime.UtcNow.AddDays(52),
            TravellersCount = 2,
            BudgetText = "30 triệu",
            RequestNote = "Cần visa nhiều lần"
        };

        var result = await controller.RequestQuote(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);

        var lead = Assert.Single(await leadRepository.GetAllAsync());
        Assert.Equal("Visa", lead.ServiceType);
        Assert.Equal("New", lead.Status);
        Assert.Equal("Open", lead.QuoteStatus);
        Assert.Equal("Nhật Bản", lead.Destination);
        Assert.True(lead.SlaDueAt > DateTime.UtcNow);

        var notification = Assert.Single(await notificationRepository.GetAllAsync());
        Assert.Contains("Visa", notification.Title);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
