using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;
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

        public async Task<IActionResult> Index(
            string status = "all", 
            int page = 1, 
            int pageSize = 10, 
            string searchString = "",
            string sortOrder = "")
        {
            // Ensure valid page size
            if (pageSize < 5) pageSize = 10;
            if (pageSize > 100) pageSize = 100;
            
            // Sort Params
            ViewBag.CurrentSort = sortOrder;
            ViewBag.NameSortParm = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewBag.PriceSortParm = sortOrder == "price" ? "price_desc" : "price";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";

            var statusLower = status.ToLower();
            string? targetStatus = null;
            if (statusLower == "active") targetStatus = "Active";
            else if (statusLower == "draft") targetStatus = "Draft";
            else if (statusLower == "soldout") targetStatus = "SoldOut";
            else if (statusLower == "comingsoon") targetStatus = "ComingSoon";
            else if (statusLower == "hidden") targetStatus = "Hidden";
            else if (statusLower == "deleted") targetStatus = "Deleted";
            // If "all" or unknown, targetStatus stays null, implying "all query"

            var search = !string.IsNullOrEmpty(searchString) ? searchString.Trim().ToLower() : null;
            bool isObjectId = !string.IsNullOrEmpty(search) && MongoDB.Bson.ObjectId.TryParse(search, out _);

            System.Linq.Expressions.Expression<Func<Tour, bool>> filter;

            if (!string.IsNullOrEmpty(search))
            {
                filter = t => 
                    (targetStatus == null ? t.Status != "Deleted" : t.Status == targetStatus) &&
                    (
                        t.Name.ToLower().Contains(search) || 
                        (t.Destination != null && t.Destination.City != null && t.Destination.City.ToLower().Contains(search)) ||
                        (isObjectId && t.Id == search) // Exact ID match only
                    );
            }
            else
            {
                filter = t => (targetStatus == null ? t.Status != "Deleted" : t.Status == targetStatus);
            }

            // Fetch ALL matching items to Sort in Memory
            var toursList = await _tourRepository.FindAsync(filter);

            // Apply Sorting
            switch (sortOrder)
            {
                case "name_desc":
                    toursList = toursList.OrderByDescending(t => t.Name);
                    break;
                case "price": // Low to High
                    toursList = toursList.OrderBy(t => t.Price.Adult);
                    break;
                case "price_desc": // High to Low
                    toursList = toursList.OrderByDescending(t => t.Price.Adult);
                    break;
                case "status":
                    toursList = toursList.OrderBy(t => t.Status);
                    break;
                case "status_desc":
                    toursList = toursList.OrderByDescending(t => t.Status);
                    break;
                default: // Default: Name Ascending
                    if(string.IsNullOrEmpty(sortOrder))
                         toursList = toursList.OrderBy(t => t.Name);
                    else
                         toursList = toursList.OrderBy(t => t.Name);
                    break;
            }

            // Pagination
            var totalCount = toursList.Count();
            var items = toursList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var pagedTours = new PaginatedResult<Tour>(items, totalCount, page, pageSize);

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentPageSize"] = pageSize;
            ViewData["CurrentSearch"] = searchString;
            
            return View(pagedTours);
        }

        [HttpPost("SoftDelete/{id}")]
        public async Task<IActionResult> SoftDelete(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
            if (tour != null)
            {
                tour.Status = "Deleted";
                await _tourRepository.UpdateAsync(id, tour);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("BulkDelete")]
        public async Task<IActionResult> BulkDelete([FromForm] List<string> ids)
        {
            if (ids == null || !ids.Any()) return RedirectToAction(nameof(Index));

            foreach (var id in ids)
            {
                var tour = await _tourRepository.GetByIdAsync(id);
                if (tour != null)
                {
                    tour.Status = "Deleted";
                    await _tourRepository.UpdateAsync(id, tour);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("BulkUpdateStatus")]
        public async Task<IActionResult> BulkUpdateStatus([FromForm] List<string> ids, [FromForm] string newStatus)
        {
            if (ids == null || !ids.Any() || string.IsNullOrEmpty(newStatus)) return RedirectToAction(nameof(Index));

            foreach (var id in ids)
            {
                var tour = await _tourRepository.GetByIdAsync(id);
                if (tour != null)
                {
                    tour.Status = newStatus;
                    await _tourRepository.UpdateAsync(id, tour);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Restore/{id}")]
        public async Task<IActionResult> Restore(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
            if (tour != null)
            {
                tour.Status = "Draft"; // Restore to Draft to be safe
                await _tourRepository.UpdateAsync(id, tour);
            }
            return RedirectToAction(nameof(Index), new { status = "deleted" });
        }

        [HttpGet("Create")]
        public async Task<IActionResult> Create(string? sourceId = null)
        {
            if (!string.IsNullOrEmpty(sourceId))
            {
                var sourceTour = await _tourRepository.GetByIdAsync(sourceId);
                if (sourceTour != null)
                {
                    // Clone logic
                    sourceTour.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                    sourceTour.Name = $"{sourceTour.Name} (Copy)";
                    sourceTour.Status = "Draft";
                    sourceTour.CreatedAt = DateTime.UtcNow;
                    sourceTour.UpdatedAt = DateTime.UtcNow;
                    // Reset or keep other fields as needed
                    return View(sourceTour);
                }
            }
            return View(new Tour { Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(), Status = "Draft" });
        }

        [HttpPost("Create")]
        public async Task<IActionResult> Create(Tour tour, string? saveAction)
        {
            // Manual validation or binding checks can be added here
            if (tour.Id == null) tour.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            
            // Ensure nested objects are not null if they were completely missing from form
            if (tour.Destination == null) tour.Destination = new Destination();
            if (tour.Price == null) tour.Price = new TourPrice();
            if (tour.Duration == null) tour.Duration = new TourDuration();

            // Set status based on button clicked, default to Draft if not specified
            tour.Status = !string.IsNullOrEmpty(saveAction) ? saveAction : "Draft";
            
            tour.CreatedAt = DateTime.UtcNow;
            tour.UpdatedAt = DateTime.UtcNow;

            await _tourRepository.AddAsync(tour);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Clone/{id}")]
        public async Task<IActionResult> Clone(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
            if (tour != null)
            {
                tour.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                tour.Name = $"{tour.Name} (Copy)";
                tour.Status = "Draft";
                await _tourRepository.AddAsync(tour);
            }
             return RedirectToAction(nameof(Index));
        }

        [HttpPost("BulkClone")]
        public async Task<IActionResult> BulkClone([FromForm] List<string> ids)
        {
            if (ids == null || !ids.Any()) return RedirectToAction(nameof(Index));

            foreach (var id in ids)
            {
                var tour = await _tourRepository.GetByIdAsync(id);
                if (tour != null)
                {
                    tour.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
                    tour.Name = $"{tour.Name} (Copy)";
                    tour.Status = "Draft";
                    await _tourRepository.AddAsync(tour);
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
             var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
             return View(tour);
        }

        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Tour tour, string? saveAction)
        {
            if (id != tour.Id) return BadRequest();

            var existingTour = await _tourRepository.GetByIdAsync(id);
            if (existingTour == null) return NotFound();

            // Preserve critical fields not in form
            tour.CreatedAt = existingTour.CreatedAt;
            
            // Update status functionality: Priority: saveAction button > form selection > existing status
            tour.Status = !string.IsNullOrEmpty(saveAction) ? saveAction : (tour.Status ?? existingTour.Status);
            
            tour.UpdatedAt = DateTime.UtcNow;

            // Ensure nested objects init
            if (tour.Destination == null) tour.Destination = new Destination();
            if (tour.Price == null) tour.Price = new TourPrice();
            if (tour.Duration == null) tour.Duration = new TourDuration();

            await _tourRepository.UpdateAsync(id, tour);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("UpdateStatus/{id}")]
        public async Task<IActionResult> UpdateStatus(string id, string status)
        {
             var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();

             tour.Status = status;
             tour.UpdatedAt = DateTime.UtcNow;
             
             await _tourRepository.UpdateAsync(id, tour);
             return RedirectToAction(nameof(Details), new { id = id });
        }

        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
            return View(tour);
        }

        [HttpPost("DeleteForever/{id}")]
        public async Task<IActionResult> DeleteForever(string id)
        {
            await _tourRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index), new { status = "deleted" });
        }

        [HttpGet("Export")]
        public async Task<IActionResult> Export(string format = "json", string ids = "")
        {
            var idList = !string.IsNullOrEmpty(ids) ? ids.Split(',').ToList() : new List<string>();
            IEnumerable<Tour> tours;

            if (idList.Any())
            {
                // In a real app we'd have a GetByIdsAsync, loop for now
                var list = new List<Tour>();
                foreach(var id in idList)
                {
                   var t = await _tourRepository.GetByIdAsync(id);
                   if(t != null) list.Add(t);
                }
                tours = list;
            }
            else
            {
                // Export all (paged logic bypassed, getting all might be heavy but okay for this scope)
                 var paged = await _tourRepository.GetPagedAsync(1, 1000, t => t.Status != "Deleted");
                 tours = paged.Items;
            }

            if (format == "json")
            {
                var json = System.Text.Json.JsonSerializer.Serialize(tours, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", "tours_export.json");
            }
            else if (format == "csv")
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Id,Name,City,Price,Status");
                foreach (var t in tours)
                {
                    sb.AppendLine($"{t.Id},\"{t.Name}\",\"{t.Destination?.City}\",{t.Price?.Adult},{t.Status}");
                }
                return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "tours_export.csv");
            }

            return BadRequest("Invalid format");
        }

        [HttpPost("Import")]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0) return RedirectToAction(nameof(Index));

            try 
            {
                using (var stream = new System.IO.StreamReader(file.OpenReadStream()))
                {
                    var content = await stream.ReadToEndAsync();
                    if (file.FileName.EndsWith(".json"))
                    {
                        var tours = System.Text.Json.JsonSerializer.Deserialize<List<Tour>>(content);
                        if (tours != null)
                        {
                            foreach (var t in tours)
                            {
                                t.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(); // Always new ID for import to avoid conflict
                                await _tourRepository.AddAsync(t);
                            }
                        }
                    }
                    // Simple CSV import could go here but JSON is safer for complex objects
                }
            }
            catch
            {
               // log error
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
