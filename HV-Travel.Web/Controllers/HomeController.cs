using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Web.Models;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;

namespace HVTravel.Web.Controllers;

public class HomeController : Controller
{
    private readonly IRepository<Tour> _tourRepository;

    public HomeController(IRepository<Tour> tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["ActivePage"] = "Home";
        var tours = await _tourRepository.GetAllAsync();
        var featuredTours = tours
            .Where(t => t.Status == "Active")
            .OrderByDescending(t => t.Rating)
            .Take(6)
            .ToList();
        return View(featuredTours);
    }

    public IActionResult About()
    {
        ViewData["ActivePage"] = "About";
        return View();
    }

    public IActionResult Contact()
    {
        ViewData["ActivePage"] = "Contact";
        return View();
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
}
