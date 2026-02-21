using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Accountant")]
    public class PaymentsController : Controller
    {
        private readonly IRepository<Payment> _paymentRepository;

        public PaymentsController(IRepository<Payment> paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<IActionResult> Index(string sortOrder = "")
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.IdSortParm = string.IsNullOrEmpty(sortOrder) ? "id_desc" : "";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";
            ViewBag.BookingIdSortParm = sortOrder == "booking" ? "booking_desc" : "booking";
            ViewBag.MethodSortParm = sortOrder == "method" ? "method_desc" : "method";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.AmountSortParm = sortOrder == "amount" ? "amount_desc" : "amount";

            var payments = await _paymentRepository.GetAllAsync();

            payments = sortOrder switch
            {
                "id_desc" => payments.OrderByDescending(p => p.TransactionId),
                "date" => payments.OrderBy(p => p.PaymentDate),
                "date_desc" => payments.OrderByDescending(p => p.PaymentDate),
                "booking" => payments.OrderBy(p => p.BookingId),
                "booking_desc" => payments.OrderByDescending(p => p.BookingId),
                "method" => payments.OrderBy(p => p.PaymentMethod),
                "method_desc" => payments.OrderByDescending(p => p.PaymentMethod),
                "status" => payments.OrderBy(p => p.Status),
                "status_desc" => payments.OrderByDescending(p => p.Status),
                "amount" => payments.OrderBy(p => p.Amount),
                "amount_desc" => payments.OrderByDescending(p => p.Amount),
                _ => payments.OrderBy(p => p.TransactionId)
            };

            return View(payments);
        }
    }
}
