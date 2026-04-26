using BCrypt.Net;
using HVTravel.Application.Interfaces;
using HVTravel.Application.Models;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(AuthenticationSchemes = AuthSchemes.AdminScheme)]
    public class UsersController : Controller
    {
        private readonly IRepository<User> _userRepository;
        private readonly IAdminUserSearchService? _adminUserSearchService;
        private readonly ISearchIndexingService? _searchIndexingService;

        public UsersController(
            IRepository<User> userRepository,
            IAdminUserSearchService? adminUserSearchService = null,
            ISearchIndexingService? searchIndexingService = null)
        {
            _userRepository = userRepository;
            _adminUserSearchService = adminUserSearchService;
            _searchIndexingService = searchIndexingService;
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(
            string status = "all",
            string searchQuery = "",
            string roleFilter = "",
            string sortOrder = "",
            int page = 1,
            int pageSize = 10)
        {
            if (_adminUserSearchService != null)
            {
                var result = await _adminUserSearchService.SearchAsync(new AdminUserSearchRequest
                {
                    Status = status,
                    SearchQuery = searchQuery,
                    RoleFilter = roleFilter,
                    SortOrder = sortOrder,
                    Page = page,
                    PageSize = pageSize
                });

                ApplySortViewBags(sortOrder);
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentSearch"] = searchQuery;
                ViewData["CurrentRole"] = roleFilter;
                ViewData["CurrentPageSize"] = result.PageSize;

                return View(new UserIndexViewModel
                {
                    Users = result.Users,
                    Pagination = new PaginationMetadata
                    {
                        CurrentPage = result.CurrentPage,
                        TotalPages = result.TotalPages,
                        PageSize = result.PageSize,
                        TotalCount = result.TotalCount
                    },
                    Stats = new UserStatsSummary
                    {
                        TotalUsers = result.TotalUsers,
                        ActiveCount = result.ActiveCount,
                        InactiveCount = result.InactiveCount,
                        RoleCounts = result.RoleCounts.ToDictionary(item => item.Key, item => item.Value)
                    }
                });
            }

            var allUsersList = (await _userRepository.GetAllAsync()).ToList();
            var users = allUsersList.AsEnumerable();

            if (status == "active")
            {
                users = users.Where(u => u.Status == "Active");
            }
            else if (status == "inactive")
            {
                users = users.Where(u => u.Status == "Inactive");
            }

            if (!string.IsNullOrEmpty(searchQuery))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                users = users.Where(u => u.Role == roleFilter);
            }

            ApplySortViewBags(sortOrder);
            users = sortOrder switch
            {
                "account_desc" => users.OrderByDescending(u => u.FullName),
                "email" => users.OrderBy(u => u.Email),
                "email_desc" => users.OrderByDescending(u => u.Email),
                "role" => users.OrderBy(u => u.Role),
                "role_desc" => users.OrderByDescending(u => u.Role),
                "status" => users.OrderBy(u => u.Status),
                "status_desc" => users.OrderByDescending(u => u.Status),
                "date" => users.OrderBy(u => u.LastLogin ?? DateTime.MinValue),
                "date_desc" => users.OrderByDescending(u => u.LastLogin ?? DateTime.MinValue),
                _ => users.OrderBy(u => u.FullName)
            };

            var stats = new UserStatsSummary
            {
                TotalUsers = allUsersList.Count,
                ActiveCount = allUsersList.Count(u => u.Status == "Active"),
                InactiveCount = allUsersList.Count(u => u.Status == "Inactive"),
                RoleCounts = allUsersList
                    .GroupBy(u => u.Role)
                    .ToDictionary(group => group.Key, group => group.Count())
            };

            var totalCount = users.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentRole"] = roleFilter;
            ViewData["CurrentPageSize"] = pageSize;

            return View(new UserIndexViewModel
            {
                Users = items,
                Pagination = new PaginationMetadata
                {
                    CurrentPage = page,
                    TotalPages = totalPages,
                    PageSize = pageSize,
                    TotalCount = totalCount
                },
                Stats = stats
            });
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(User user, string password, string confirmPassword)
        {
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("password", "Mật khẩu là bắt buộc");
                return View(user);
            }

            if (password != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                return View(user);
            }

            var existingUser = await _userRepository.FindAsync(u => u.Email == user.Email);
            if (existingUser.Any())
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại");
                return View(user);
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.Status = "Active";

            await _userRepository.AddAsync(user);
            await SyncSearchUpsertAsync(user);
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole == "Manager" && user.Role == "Admin")
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa tài khoản Admin!";
                return RedirectToAction(nameof(Index));
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(string id, User user, string password, string confirmPassword)
        {
            var existingUser = await _userRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }

            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole == "Manager" && existingUser.Role == "Admin")
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa tài khoản Admin!";
                return RedirectToAction(nameof(Index));
            }

            existingUser.FullName = user.FullName;
            existingUser.Role = user.Role;
            existingUser.Status = user.Status;
            existingUser.AvatarUrl = user.AvatarUrl;

            if (!string.IsNullOrEmpty(password))
            {
                if (password != confirmPassword)
                {
                    ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");
                    return View(user);
                }

                existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            }

            await _userRepository.UpdateAsync(id, existingUser);
            await SyncSearchUpsertAsync(existingUser);
            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = "Inactive";
            await _userRepository.UpdateAsync(id, user);
            await SyncSearchUpsertAsync(user);

            TempData["Success"] = "Đã vô hiệu hóa tài khoản!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Restore(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = "Active";
            await _userRepository.UpdateAsync(id, user);
            await SyncSearchUpsertAsync(user);

            TempData["Success"] = "Đã khôi phục tài khoản!";
            return RedirectToAction(nameof(Index), new { status = "active" });
        }

        private void ApplySortViewBags(string sortOrder)
        {
            ViewBag.CurrentSort = sortOrder;
            ViewBag.AccountSortParm = string.IsNullOrEmpty(sortOrder) ? "account_desc" : "";
            ViewBag.EmailSortParm = sortOrder == "email" ? "email_desc" : "email";
            ViewBag.RoleSortParm = sortOrder == "role" ? "role_desc" : "role";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";
        }

        private Task SyncSearchUpsertAsync(User? user)
        {
            return _searchIndexingService?.UpsertUserAsync(user) ?? Task.CompletedTask;
        }
    }
}
