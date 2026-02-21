using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using HVTravel.Domain.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Web.Models;
using BCrypt.Net;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class UsersController : Controller
    {
        private readonly IRepository<User> _userRepository;

        public UsersController(IRepository<User> userRepository)
        {
            _userRepository = userRepository;
        }

        // Index - Chỉ Admin và Manager được xem
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Index(
            string status = "all",
            string searchQuery = "",
            string roleFilter = "",
            string sortOrder = "",
            int page = 1,
            int pageSize = 10)
        {
            var users = await _userRepository.GetAllAsync();

            // Filter by status tab
            if (status == "active")
            {
                users = users.Where(u => u.Status == "Active");
            }
            else if (status == "inactive")
            {
                users = users.Where(u => u.Status == "Inactive");
            }

            // Filter by search query
            if (!string.IsNullOrEmpty(searchQuery))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            // Filter by role
            if (!string.IsNullOrEmpty(roleFilter))
            {
                users = users.Where(u => u.Role == roleFilter);
            }

            // Sorting ViewBags
            ViewBag.CurrentSort = sortOrder;
            ViewBag.AccountSortParm = string.IsNullOrEmpty(sortOrder) ? "account_desc" : "";
            ViewBag.EmailSortParm = sortOrder == "email" ? "email_desc" : "email";
            ViewBag.RoleSortParm = sortOrder == "role" ? "role_desc" : "role";
            ViewBag.StatusSortParm = sortOrder == "status" ? "status_desc" : "status";
            ViewBag.DateSortParm = sortOrder == "date" ? "date_desc" : "date";

            // Sort
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

            // Stats
            var allUsers = await _userRepository.GetAllAsync();
            var stats = new UserStatsSummary
            {
                TotalUsers = allUsers.Count(),
                ActiveCount = allUsers.Count(u => u.Status == "Active"),
                InactiveCount = allUsers.Count(u => u.Status == "Inactive"),
                RoleCounts = allUsers.GroupBy(u => u.Role)
                    .ToDictionary(g => g.Key, g => g.Count())
            };

            // Pagination
            var totalCount = users.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            var items = users.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var pagination = new PaginationMetadata
            {
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalCount = totalCount
            };

            // ViewBag for filters
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentSearch"] = searchQuery;
            ViewData["CurrentRole"] = roleFilter;
            ViewData["CurrentPageSize"] = pageSize;

            var viewModel = new UserIndexViewModel
            {
                Users = items,
                Pagination = pagination,
                Stats = stats
            };

            return View(viewModel);
        }

        // Create - Chỉ Admin và Manager
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
            // Password validation
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

            // Check email exists
            var existingUser = await _userRepository.FindAsync(u => u.Email == user.Email);
            if (existingUser.Any())
            {
                ModelState.AddModelError("Email", "Email này đã tồn tại");
                return View(user);
            }

            // Hash password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;
            user.Status = "Active";

            await _userRepository.AddAsync(user);
            TempData["Success"] = "Tạo tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Edit - Chỉ Admin và Manager (Manager không được sửa tài khoản Admin)
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();
            
            // Manager không được sửa tài khoản Admin
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
            if (existingUser == null) return NotFound();
            
            // Manager không được sửa tài khoản Admin
            var currentUserRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (currentUserRole == "Manager" && existingUser.Role == "Admin")
            {
                TempData["Error"] = "Bạn không có quyền chỉnh sửa tài khoản Admin!";
                return RedirectToAction(nameof(Index));
            }

            // Update fields
            existingUser.FullName = user.FullName;
            existingUser.Role = user.Role;
            existingUser.Status = user.Status;
            existingUser.AvatarUrl = user.AvatarUrl;

            // Update password if provided
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
            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Delete - Chỉ Admin và Manager (Manager không được xóa tài khoản Admin)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            // Soft delete - set status to Inactive
            user.Status = "Inactive";
            await _userRepository.UpdateAsync(id, user);

            TempData["Success"] = "Đã vô hiệu hóa tài khoản!";
            return RedirectToAction(nameof(Index));
        }

        // Restore - Chỉ Admin và Manager
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Restore(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null) return NotFound();

            user.Status = "Active";
            await _userRepository.UpdateAsync(id, user);

            TempData["Success"] = "Đã khôi phục tài khoản!";
            return RedirectToAction(nameof(Index), new { status = "active" });
        }
    }
}
