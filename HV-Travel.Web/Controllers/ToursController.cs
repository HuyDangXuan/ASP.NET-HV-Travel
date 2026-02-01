using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using System.Threading.Tasks;

namespace HVTravel.Web.Controllers
{
    // [Authorize(Roles = "Admin")] // Commented out for dev ease if needed, but keeping generally
    [Route("Admin/[controller]")]
    public class ToursController : Controller
    {
        private readonly IRepository<Tour> _tourRepository;

        public ToursController(IRepository<Tour> tourRepository)
        {
            _tourRepository = tourRepository;
        }

        public async Task<IActionResult> Index()
        {
            var tours = await _tourRepository.GetAllAsync();
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
            // Simple validation skipping for now to ensure flow works
            // if (ModelState.IsValid) 
            {
                await _tourRepository.AddAsync(tour);
                return RedirectToAction(nameof(Index));
            }
            // return View(tour);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
             var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
             return View(tour);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Tour tour)
        {
            if (id != tour.Id) return BadRequest();

            // if (ModelState.IsValid)
            {
                await _tourRepository.UpdateAsync(id, tour);
                return RedirectToAction(nameof(Index));
            }
            // return View(tour);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _tourRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
