using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;

        public PaymentsController(IRepository<Payment> paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<IActionResult> Index()
        {
            var payments = await _paymentRepository.GetAllAsync();
            return View(payments);
        }
    }
}
