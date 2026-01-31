using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VietVoyage.Application.Interfaces;
using VietVoyage.Domain.Entities;

namespace VietVoyage.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class ToursController : Controller
    {
        private readonly ITourService _tourService;

        public ToursController(ITourService tourService)
        {
            _tourService = tourService;
        }

        public async Task<IActionResult> Index()
        {
            var tours = await _tourService.GetAllToursAsync();
            return View(tours);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Tour tour)
        {
            if (ModelState.IsValid)
            {
                await _tourService.CreateTourAsync(tour);
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        [HttpGet("Edit/{id}")]
        public IActionResult Edit(string id)
        {
            // var tour = await _tourService.GetTourByIdAsync(id);
            // if (tour == null) return NotFound();
            // return View(tour);
            return View();
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Tour tour)
        {
            if (id != tour.Id) return BadRequest();

            if (ModelState.IsValid)
            {
                await _tourService.UpdateTourAsync(tour);
                return RedirectToAction(nameof(Index));
            }
            return View(tour);
        }

        [HttpGet("Details/{id}")]
        public IActionResult Details(string id)
        {
            return View();
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _tourService.DeleteTourAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
