using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Features;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Models;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Application.Services;
using HVTravel.Web.Models;
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
        private readonly ITourRepository _tourRepository;
        private readonly ITourSearchIndexingService? _tourSearchIndexingService;
        private readonly IAdminTourSearchService? _adminTourSearchService;

        public ToursController(
            ITourRepository tourRepository,
            ITourSearchIndexingService? tourSearchIndexingService = null,
            IAdminTourSearchService? adminTourSearchService = null)
        {
            _tourRepository = tourRepository;
            _tourSearchIndexingService = tourSearchIndexingService;
            _adminTourSearchService = adminTourSearchService;
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

            if (_adminTourSearchService != null)
            {
                var indexedTours = await _adminTourSearchService.SearchAsync(new AdminTourSearchRequest
                {
                    Status = status,
                    Page = page,
                    PageSize = pageSize,
                    SearchString = searchString,
                    SortOrder = sortOrder
                });

                ViewData["CurrentStatus"] = status;
                ViewData["CurrentPageSize"] = pageSize;
                ViewData["CurrentSearch"] = searchString;
                return View(indexedTours);
            }

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
                await SyncSearchUpsertAsync(tour);
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
                    await SyncSearchUpsertAsync(tour);
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
                    await SyncSearchUpsertAsync(tour);
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
                await SyncSearchUpsertAsync(tour);
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
            if (string.IsNullOrWhiteSpace(tour.Id))
            {
                tour.Id = ObjectId.GenerateNewId().ToString();
            }

            PrepareTourForLegacyAdminPersistence(tour, !string.IsNullOrEmpty(saveAction) ? saveAction : "Draft");
            EnsureTourStructure(tour);
            tour.Status = !string.IsNullOrEmpty(saveAction) ? saveAction : "Draft";
            tour.Slug = string.IsNullOrWhiteSpace(tour.Slug) ? GenerateSlug(tour.Name) : tour.Slug.Trim();
            tour.ConfirmationType = string.IsNullOrWhiteSpace(tour.ConfirmationType) ? "Instant" : tour.ConfirmationType.Trim();
            tour.CreatedAt = DateTime.UtcNow;
            tour.UpdatedAt = DateTime.UtcNow;

            NormalizeTourCollections(tour);
            ValidateTourCollections(tour);
            if (!ModelState.IsValid)
            {
                return View(tour);
            }

            NormalizeRichTextFields(tour);

            await _tourRepository.AddAsync(tour);
            await SyncSearchUpsertAsync(tour);
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
                await SyncSearchUpsertAsync(tour);
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
                    await SyncSearchUpsertAsync(tour);
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

            EnsureTourStructure(existingTour);
            EnsureTourStructure(tour);

            var form = HttpContext?.Features.Get<IFormFeature>()?.Form;
            var versionProvided = form?.ContainsKey(nameof(Tour.Version)) == true;
            var departuresPosted = ShouldApplyPostedDepartures(form, tour.Departures);
            var routingPosted = ShouldApplyPostedRouting(form, tour.Routing);

            MergeEditableFields(existingTour, tour, saveAction, versionProvided, departuresPosted, routingPosted);
            NormalizeTourCollections(tour);
            ValidateTourCollections(tour);
            if (!ModelState.IsValid)
            {
                return View(tour);
            }

            NormalizeRichTextFields(tour);

            await _tourRepository.UpdateAsync(id, tour);
            await SyncSearchUpsertAsync(tour);
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
             await SyncSearchUpsertAsync(tour);
             return RedirectToAction(nameof(Details), new { id = id });
        }

        // Details - Tất cả roles đều xem được
        [Authorize(Roles = "Admin,Manager,Staff,Guide")]
        [HttpGet("Details/{id}")]
        public async Task<IActionResult> Details(string id)
        {
            var tour = await _tourRepository.GetByIdAsync(id);
             if (tour == null) return NotFound();
            ViewData["RouteInsight"] = new RouteInsightService().Build(tour);
            return View(tour);
        }

        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost("OptimizeRoutingPreview")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> OptimizeRoutingPreview([FromBody] RouteOptimizationPreviewRequest request)
        {
            if (request == null)
            {
                return Task.FromResult<IActionResult>(BadRequest(new RouteOptimizationPreviewErrorResponse
                {
                    Errors = ["Preview request is required."]
                }));
            }

            ModelState.Clear();

            var draftTour = BuildOptimizationPreviewTour(request);
            EnsureTourStructure(draftTour);
            NormalizeTourCollections(draftTour);
            ValidateTourCollections(draftTour);

            if (!ModelState.IsValid)
            {
                return Task.FromResult<IActionResult>(BadRequest(new RouteOptimizationPreviewErrorResponse
                {
                    Errors = CollectModelStateErrors()
                }));
            }

            var result = new RouteOptimizationService(new RouteInsightService()).Optimize(draftTour, new RouteOptimizationRequest
            {
                Profile = request.Profile
            });
            var response = new RouteOptimizationPreviewResponse
            {
                CanOptimize = result.CanOptimize,
                Profile = result.Profile,
                ProfileLabel = result.ProfileLabel,
                ProfileDescription = result.ProfileDescription,
                CurrentObjectiveScore = result.CurrentObjectiveScore,
                SuggestedObjectiveScore = result.SuggestedObjectiveScore,
                CurrentInsight = result.CurrentInsight,
                SuggestedInsight = result.SuggestedInsight,
                Assignments = result.Assignments,
                Days = result.Days,
                Warnings = result.Warnings,
                UnchangedReason = result.UnchangedReason
            };

            return Task.FromResult<IActionResult>(Ok(response));
        }

        // DeleteForever - Admin, Manager, Staff (Guide không có quyền)
        [Authorize(Roles = "Admin,Manager,Staff")]
        [HttpPost("DeleteForever/{id}")]
        public async Task<IActionResult> DeleteForever(string id)
        {
            await _tourRepository.DeleteAsync(id);
            await SyncSearchDeleteAsync(id);
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
                tours = await _tourRepository.GetByIdsAsync(idList);
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
                                t.Id = ObjectId.GenerateNewId().ToString();
                                PrepareTourForLegacyAdminPersistence(t, string.IsNullOrWhiteSpace(t.Status) ? "Draft" : t.Status);
                                EnsureTourStructure(t);
                                t.Slug = string.IsNullOrWhiteSpace(t.Slug) ? GenerateSlug(t.Name) : t.Slug.Trim();
                                t.ConfirmationType = string.IsNullOrWhiteSpace(t.ConfirmationType) ? "Instant" : t.ConfirmationType.Trim();
                                NormalizeTourCollections(t);
                                await _tourRepository.AddAsync(t);
                                await SyncSearchUpsertAsync(t);
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
            tour.Status = status;

            if (string.IsNullOrWhiteSpace(tour.ConfirmationType))
            {
                tour.ConfirmationType = "Instant";
            }

            if (string.IsNullOrWhiteSpace(tour.Slug))
            {
                tour.Slug = GenerateSlug(tour.Name);
            }
        }

        private static void EnsureTourStructure(Tour tour)
        {
            tour.Destination ??= new Destination();
            tour.Price ??= new TourPrice();
            tour.Duration ??= new TourDuration();
            tour.Images ??= new List<string>();
            tour.StartDates ??= new List<DateTime>();
            tour.Schedule ??= new List<ScheduleItem>();
            tour.GeneratedInclusions ??= new List<string>();
            tour.GeneratedExclusions ??= new List<string>();
            tour.Seo ??= new SeoMetadata();
            tour.CancellationPolicy ??= new TourCancellationPolicy();
            tour.Highlights ??= new List<string>();
            tour.BadgeSet ??= new List<string>();
            tour.SupplierRef ??= new SupplierReference();
            tour.Departures ??= new List<TourDeparture>();

            if (tour.Routing != null)
            {
                tour.Routing.Stops ??= new List<TourRouteStop>();
            }
        }

        private void MergeEditableFields(
            Tour existingTour,
            Tour postedTour,
            string? saveAction,
            bool versionProvided,
            bool departuresPosted,
            bool routingPosted)
        {
            postedTour.Id = existingTour.Id;
            postedTour.Code = postedTour.Code?.Trim() ?? string.Empty;
            postedTour.Name = postedTour.Name?.Trim() ?? string.Empty;
            postedTour.Description ??= string.Empty;
            postedTour.ShortDescription ??= string.Empty;
            postedTour.Destination ??= new Destination();
            postedTour.Images ??= new List<string>();
            postedTour.Price ??= new TourPrice();
            postedTour.Duration ??= new TourDuration();
            postedTour.StartDates ??= new List<DateTime>();
            postedTour.Schedule ??= new List<ScheduleItem>();
            postedTour.GeneratedInclusions ??= new List<string>();
            postedTour.GeneratedExclusions ??= new List<string>();
            postedTour.Status = !string.IsNullOrWhiteSpace(saveAction) ? saveAction : (postedTour.Status ?? existingTour.Status);
            postedTour.CreatedAt = existingTour.CreatedAt;
            postedTour.UpdatedAt = DateTime.UtcNow;
            postedTour.Slug = existingTour.Slug;
            postedTour.Seo = existingTour.Seo;
            postedTour.CancellationPolicy = existingTour.CancellationPolicy;
            postedTour.ConfirmationType = existingTour.ConfirmationType;
            postedTour.Highlights = existingTour.Highlights;
            postedTour.MeetingPoint = existingTour.MeetingPoint;
            postedTour.BadgeSet = existingTour.BadgeSet;
            postedTour.SupplierRef = existingTour.SupplierRef;

            if (!departuresPosted)
            {
                postedTour.Departures = existingTour.Departures;
            }

            if (!routingPosted)
            {
                postedTour.Routing = existingTour.Routing;
            }

            if (versionProvided)
            {
                postedTour.Version = postedTour.Version;
            }
            else
            {
                postedTour.Version = existingTour.Version;
            }
        }

        private static bool ShouldApplyPostedDepartures(IFormCollection? form, List<TourDeparture>? departures)
        {
            return form?.ContainsKey("__departuresEditor") == true
                || (departures?.Any(IsMeaningfulDeparture) == true);
        }

        private static bool ShouldApplyPostedRouting(IFormCollection? form, TourRouting? routing)
        {
            return form?.ContainsKey("__routingEditor") == true
                || (routing?.Stops?.Any(IsMeaningfulRouteStop) == true);
        }

        private static Tour BuildOptimizationPreviewTour(RouteOptimizationPreviewRequest request)
        {
            return new Tour
            {
                Id = ObjectId.GenerateNewId().ToString(),
                Status = "Draft",
                Schedule = (request.Schedule ?? new List<RouteOptimizationPreviewScheduleItem>())
                    .Select(item => new ScheduleItem
                    {
                        Day = item.Day,
                        Title = item.Title?.Trim() ?? string.Empty,
                        Description = item.Description ?? string.Empty,
                        Activities = new List<string>()
                    })
                    .ToList(),
                Routing = new TourRouting
                {
                    SchemaVersion = 1,
                    Stops = (request.Stops ?? new List<RouteOptimizationPreviewStop>())
                        .Select(item => new TourRouteStop
                        {
                            Id = string.IsNullOrWhiteSpace(item.ClientKey) ? Guid.NewGuid().ToString("N") : item.ClientKey.Trim(),
                            Day = item.Day,
                            Order = item.Order,
                            Name = item.Name?.Trim() ?? string.Empty,
                            Type = item.Type?.Trim() ?? string.Empty,
                            VisitMinutes = item.VisitMinutes,
                            AttractionScore = item.AttractionScore,
                            Note = item.Note?.Trim() ?? string.Empty,
                            Coordinates = new GeoPoint
                            {
                                Lat = item.Coordinates?.Lat,
                                Lng = item.Coordinates?.Lng
                            }
                        })
                        .ToList()
                }
            };
        }

        private IReadOnlyList<string> CollectModelStateErrors()
        {
            return ModelState.Values
                .SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage)
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .ToList();
        }

        private void NormalizeTourCollections(Tour tour)
        {
            tour.Images = (tour.Images ?? new List<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .ToList();

            tour.StartDates = (tour.StartDates ?? new List<DateTime>())
                .Where(value => value != default)
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            tour.Schedule = (tour.Schedule ?? new List<ScheduleItem>())
                .Where(item => item != null)
                .Select(item =>
                {
                    item.Title = item.Title?.Trim() ?? string.Empty;
                    item.Description = item.Description ?? string.Empty;
                    item.Activities ??= new List<string>();
                    item.Activities = item.Activities
                        .Where(value => !string.IsNullOrWhiteSpace(value))
                        .Select(value => value.Trim())
                        .ToList();
                    return item;
                })
                .Where(item => item.Day > 0 || !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Description) || item.Activities.Count > 0)
                .OrderBy(item => item.Day)
                .ToList();

            tour.Departures = (tour.Departures ?? new List<TourDeparture>())
                .Where(item => item != null)
                .Where(IsMeaningfulDeparture)
                .Select(item =>
                {
                    item.Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim();
                    item.ConfirmationType = string.IsNullOrWhiteSpace(item.ConfirmationType) ? "Instant" : item.ConfirmationType.Trim();
                    item.Status = string.IsNullOrWhiteSpace(item.Status) ? "Scheduled" : item.Status.Trim();
                    return item;
                })
                .OrderBy(item => item.StartDate)
                .ToList();

            if (tour.Routing?.Stops == null)
            {
                tour.Routing = null;
                return;
            }

            var stops = tour.Routing.Stops
                .Where(item => item != null)
                .Where(IsMeaningfulRouteStop)
                .Select(item =>
                {
                    item.Id = string.IsNullOrWhiteSpace(item.Id) ? Guid.NewGuid().ToString("N") : item.Id.Trim();
                    item.Name = item.Name?.Trim() ?? string.Empty;
                    item.Type = item.Type?.Trim() ?? string.Empty;
                    item.Note = item.Note?.Trim() ?? string.Empty;
                    item.Coordinates ??= new GeoPoint();
                    return item;
                })
                .OrderBy(item => item.Day)
                .ThenBy(item => item.Order)
                .ToList();

            if (stops.Count == 0)
            {
                tour.Routing = null;
                return;
            }

            tour.Routing.SchemaVersion = 1;
            tour.Routing.Stops = stops;
        }

        private void ValidateTourCollections(Tour tour)
        {
            foreach (var departure in tour.Departures ?? new List<TourDeparture>())
            {
                if (departure.StartDate == default)
                {
                    ModelState.AddModelError("Departures", "Mỗi departure hợp lệ phải có ngày khởi hành.");
                }

                if (departure.Capacity < 0 || departure.BookedCount < 0 || departure.CutoffHours < 0)
                {
                    ModelState.AddModelError("Departures", "Capacity, booked count và cutoff hours không được âm.");
                }

                if (departure.BookedCount > departure.Capacity)
                {
                    ModelState.AddModelError("Departures", "Booked count không được lớn hơn capacity.");
                }
            }

            if (tour.Routing?.Stops == null || tour.Routing.Stops.Count == 0)
            {
                return;
            }

            var scheduleDays = (tour.Schedule ?? new List<ScheduleItem>())
                .Select(item => item.Day)
                .Where(day => day > 0)
                .ToHashSet();

            foreach (var stop in tour.Routing.Stops)
            {
                if (!scheduleDays.Contains(stop.Day))
                {
                    ModelState.AddModelError("Routing", "Mỗi route stop phải thuộc một ngày đã tồn tại trong schedule.");
                }

                if (stop.Order <= 0)
                {
                    ModelState.AddModelError("Routing", "Order của route stop phải lớn hơn 0.");
                }

                var hasLat = stop.Coordinates?.Lat.HasValue == true;
                var hasLng = stop.Coordinates?.Lng.HasValue == true;
                if (hasLat != hasLng)
                {
                    ModelState.AddModelError("Routing", "Lat và lng phải được nhập theo cặp.");
                }

                if (stop.VisitMinutes < 0)
                {
                    ModelState.AddModelError("Routing", "Visit minutes không được âm.");
                }

                if (stop.AttractionScore is < 0 or > 10)
                {
                    ModelState.AddModelError("Routing", "Attraction score phải nằm trong khoảng 0 đến 10.");
                }
            }
        }

        private static bool IsMeaningfulDeparture(TourDeparture departure)
        {
            return departure.StartDate != default
                   || departure.AdultPrice > 0m
                   || departure.ChildPrice > 0m
                   || departure.InfantPrice > 0m
                   || departure.DiscountPercentage > 0m
                   || departure.Capacity > 0
                   || departure.BookedCount > 0
                   || departure.CutoffHours > 0
                   || !string.IsNullOrWhiteSpace(departure.Id)
                   || !string.IsNullOrWhiteSpace(departure.ConfirmationType)
                   || !string.IsNullOrWhiteSpace(departure.Status);
        }

        private static bool IsMeaningfulRouteStop(TourRouteStop stop)
        {
            return stop.Day > 0
                   || stop.Order > 0
                   || !string.IsNullOrWhiteSpace(stop.Name)
                   || !string.IsNullOrWhiteSpace(stop.Type)
                   || !string.IsNullOrWhiteSpace(stop.Note)
                   || stop.VisitMinutes > 0
                   || stop.AttractionScore > 0
                   || stop.Coordinates?.Lat != null
                   || stop.Coordinates?.Lng != null;
        }

        private static List<Tour>? DeserializeTours(string content)
        {
            try
            {
                return BsonSerializer.Deserialize<List<Tour>>(content);
            }
            catch
            {
                return JsonSerializer.Deserialize<List<Tour>>(content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
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

            return DeserializeTours(content);
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

        private Task SyncSearchUpsertAsync(Tour? tour)
        {
            return _tourSearchIndexingService?.UpsertTourAsync(tour) ?? Task.CompletedTask;
        }

        private Task SyncSearchDeleteAsync(string? tourId)
        {
            return _tourSearchIndexingService?.DeleteTourAsync(tourId) ?? Task.CompletedTask;
        }

        private static string GenerateSlug(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return ObjectId.GenerateNewId().ToString();
            }

            var normalized = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(normalized.Length);
            foreach (var character in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            var ascii = builder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            ascii = ascii.Replace('đ', 'd');
            ascii = Regex.Replace(ascii, @"[^a-z0-9]+", "-");
            ascii = ascii.Trim('-');

            return string.IsNullOrWhiteSpace(ascii) ? ObjectId.GenerateNewId().ToString() : ascii;
        }
    }
}
