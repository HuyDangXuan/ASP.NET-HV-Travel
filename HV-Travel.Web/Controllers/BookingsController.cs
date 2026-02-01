using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using System.Threading.Tasks;

namespace HVTravel.Web.Controllers
{
    public class BookingsController : Controller
    {
        private readonly IRepository<Booking> _bookingRepository;

        public BookingsController(IRepository<Booking> bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<IActionResult> Index()
        {
            var bookings = await _bookingRepository.GetAllAsync();
            return View(bookings);
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null) return NotFound();
            return View(booking);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Booking booking)
        {
            if (id != booking.Id) return BadRequest();

            // if (ModelState.IsValid)
            {
                await _bookingRepository.UpdateAsync(id, booking);
                return RedirectToAction(nameof(Index));
            }
            // return View(booking);
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            await _bookingRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
