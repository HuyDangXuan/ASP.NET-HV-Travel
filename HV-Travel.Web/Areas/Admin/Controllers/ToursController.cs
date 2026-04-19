using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;
using System.Threading.Tasks;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme)]
    [Route("Admin/[controller]")]
    public class ToursController : Controller
    {
        private readonly IRepository<Tour> _tourRepository;

        public ToursController(IRepository<Tour> tourRepository)
        {
            _tourRepository = tourRepository;
        }

        // Index - Tất cả roles đều xem được
        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
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

        // SoftDelete - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // BulkDelete - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // BulkUpdateStatus - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // Restore - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // Create GET - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // Create POST - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost("Create")]
        public async Task<IActionResult> Create(Tour tour, string? saveAction)
        {
            // Manual validation or binding checks can be added here
            if (string.IsNullOrWhiteSpace(tour.Id))
            {
                tour.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            }

            PrepareTourForLegacyAdminPersistence(tour, !string.IsNullOrEmpty(saveAction) ? saveAction : "Draft");
            tour.CreatedAt = DateTime.UtcNow;
            tour.UpdatedAt = DateTime.UtcNow;

            NormalizeRichTextFields(tour);

            await _tourRepository.AddAsync(tour);
            return RedirectToAction(nameof(Index));
        }

        // Clone - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // BulkClone - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // Edit GET - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
             var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
             return View(tour);
        }

        // Edit POST - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost("Edit/{id}")]
        public async Task<IActionResult> Edit(string id, Tour tour, string? saveAction)
        {
            if (id != tour.Id) return BadRequest();

            var existingTour = await _tourRepository.GetByIdAsync(id);
            if (existingTour == null) return NotFound();

            var versionProvided = HttpContext?.Features.Get<IFormFeature>()?.Form?.ContainsKey(nameof(Tour.Version)) == true;
            if (versionProvided)
            {
                existingTour.Version = tour.Version;
            }

            ApplyLegacyAdminEditableFields(existingTour, tour, saveAction);
            NormalizeRichTextFields(existingTour);

            await _tourRepository.UpdateAsync(id, existingTour);
            return RedirectToAction(nameof(Index));
        }

        // UpdateStatus - Admin, Manager, Staff (Guide không có quyền)
        private static void NormalizeRichTextFields(Tour tour)
        {
            tour.Description = RichTextContentFormatter.ToTrustedHtml(tour.Description);
            tour.ShortDescription = RichTextContentFormatter.ToTrustedHtml(tour.ShortDescription);

            if (tour.Schedule == null)
            {
                return;
            }

            foreach (var item in tour.Schedule)
            {
                if (item == null)
                {
                    continue;
                }

                item.Description = RichTextContentFormatter.ToTrustedHtml(item.Description);
            }
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
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

        // Details - Tất cả roles đều xem được
        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
            return View(tour);
        }

        // DeleteForever - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost("DeleteForever/{id}")]
        public async Task<IActionResult> DeleteForever(string id)
        {
            await _tourRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index), new { status = "deleted" });
        }

        // Export - Tất cả roles đều export được
        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
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

        // Import - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
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
                        var tours = DeserializeImportedTours(content);
                        if (tours != null)
                        {
                            foreach (var t in tours)
                            {
                                t.Id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(); // Always new ID for import to avoid conflict
                                PrepareTourForLegacyAdminPersistence(t, string.IsNullOrWhiteSpace(t.Status) ? "Draft" : t.Status);
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

        private static void ApplyLegacyAdminEditableFields(Tour target, Tour source, string? saveAction)
        {
            target.Code = source.Code;
            target.Name = source.Name;
            target.Description = source.Description;
            target.ShortDescription = source.ShortDescription;
            target.Destination = source.Destination ?? new Destination();
            target.Images = source.Images ?? new List<string>();
            target.Price = source.Price ?? new TourPrice();
            target.Duration = source.Duration ?? new TourDuration();
            target.StartDates = source.StartDates ?? new List<DateTime>();
            target.Schedule = source.Schedule ?? new List<ScheduleItem>();
            target.GeneratedInclusions = source.GeneratedInclusions ?? new List<string>();
            target.GeneratedExclusions = source.GeneratedExclusions ?? new List<string>();
            target.MaxParticipants = source.MaxParticipants;
            target.CurrentParticipants = source.CurrentParticipants;
            target.Rating = source.Rating;
            target.ReviewCount = source.ReviewCount;
            target.Status = !string.IsNullOrEmpty(saveAction) ? saveAction : (source.Status ?? target.Status);
            target.UpdatedAt = DateTime.UtcNow;
        }

        private static void PrepareTourForLegacyAdminPersistence(Tour tour, string status)
        {
            tour.Destination ??= new Destination();
            tour.Price ??= new TourPrice();
            tour.Duration ??= new TourDuration();
            tour.Seo ??= new SeoMetadata();
            tour.CancellationPolicy ??= new TourCancellationPolicy();
            tour.SupplierRef ??= new SupplierReference();
            tour.Images ??= new List<string>();
            tour.StartDates ??= new List<DateTime>();
            tour.Schedule ??= new List<ScheduleItem>();
            tour.GeneratedInclusions ??= new List<string>();
            tour.GeneratedExclusions ??= new List<string>();
            tour.Highlights ??= new List<string>();
            tour.BadgeSet ??= new List<string>();
            tour.Departures ??= new List<TourDeparture>();

            if (string.IsNullOrWhiteSpace(tour.ConfirmationType))
            {
                tour.ConfirmationType = "Instant";
            }

            if (string.IsNullOrWhiteSpace(tour.Slug))
            {
                tour.Slug = GenerateSlug(tour.Name);
            }

            tour.Status = status;
        }

        private static List<Tour>? DeserializeImportedTours(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return null;
            }

            if (LooksLikeMongoExtendedJson(content))
            {
                var documents = BsonSerializer.Deserialize<BsonArray>(content)
                    .Where(item => item is BsonDocument)
                    .Select(item => item.AsBsonDocument)
                    .ToList();

                return documents.Select(DeserializeMongoExtendedTour).ToList();
            }

            return System.Text.Json.JsonSerializer.Deserialize<List<Tour>>(content, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private static bool LooksLikeMongoExtendedJson(string content)
        {
            return content.Contains("\"$oid\"", StringComparison.Ordinal)
                || content.Contains("\"$date\"", StringComparison.Ordinal)
                || content.Contains("\"$numberDecimal\"", StringComparison.Ordinal);
        }

        private static Tour DeserializeMongoExtendedTour(BsonDocument document)
        {
            var tour = BsonSerializer.Deserialize<Tour>(document);

            if (document.TryGetValue("departures", out var departuresValue) && departuresValue is BsonArray departuresArray)
            {
                tour.Departures = departuresArray
                    .Where(item => item is BsonDocument)
                    .Select(item => DeserializeMongoExtendedDeparture(item.AsBsonDocument))
                    .ToList();
            }

            if (document.TryGetValue("routing", out var routingValue) && routingValue is BsonDocument routingDocument)
            {
                tour.Routing = BsonSerializer.Deserialize<TourRouting>(routingDocument);
            }

            return tour;
        }

        private static TourDeparture DeserializeMongoExtendedDeparture(BsonDocument document)
        {
            return new TourDeparture
            {
                Id = document.TryGetValue("id", out var idValue) ? idValue.AsString : string.Empty,
                StartDate = document.TryGetValue("startDate", out var startDateValue) ? startDateValue.ToUniversalTime() : default,
                AdultPrice = document.TryGetValue("adultPrice", out var adultPriceValue) ? adultPriceValue.ToDecimal() : 0m,
                ChildPrice = document.TryGetValue("childPrice", out var childPriceValue) ? childPriceValue.ToDecimal() : 0m,
                InfantPrice = document.TryGetValue("infantPrice", out var infantPriceValue) ? infantPriceValue.ToDecimal() : 0m,
                DiscountPercentage = document.TryGetValue("discountPercentage", out var discountValue) ? discountValue.ToDecimal() : 0m,
                Capacity = document.TryGetValue("capacity", out var capacityValue) ? capacityValue.ToInt32() : 0,
                BookedCount = document.TryGetValue("bookedCount", out var bookedCountValue) ? bookedCountValue.ToInt32() : 0,
                ConfirmationType = document.TryGetValue("confirmationType", out var confirmationTypeValue) ? confirmationTypeValue.AsString : "Instant",
                Status = document.TryGetValue("status", out var statusValue) ? statusValue.AsString : "Scheduled",
                CutoffHours = document.TryGetValue("cutoffHours", out var cutoffValue) ? cutoffValue.ToInt32() : 24
            };
        }

        private static string GenerateSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var lowered = value.Trim().ToLowerInvariant();
            var buffer = new List<char>(lowered.Length);
            var previousDash = false;

            foreach (var ch in lowered)
            {
                if (char.IsLetterOrDigit(ch))
                {
                    buffer.Add(ch);
                    previousDash = false;
                    continue;
                }

                if (previousDash)
                {
                    continue;
                }

                buffer.Add('-');
                previousDash = true;
            }

            return new string(buffer.ToArray()).Trim('-');
        }
    }
}
