using System.Security.Claims;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class CustomerAuthController : Controller
{
    private readonly IRepository<Customer> _customerRepository;

    public CustomerAuthController(IRepository<Customer> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, string? email = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["Title"] = "Đăng nhập";
        ViewData["Description"] = "Đăng nhập tài khoản HV Travel để theo dõi hành trình và quản lý thông tin đặt tour.";
        return View(new CustomerLoginViewModel
        {
            ReturnUrl = returnUrl,
            Email = email ?? string.Empty
        });
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["Title"] = "Đăng ký";
        ViewData["Description"] = "Tạo tài khoản HV Travel để quản lý booking, lưu thông tin liên hệ và nhận ưu đãi du lịch.";
        return View(new CustomerRegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(CustomerLoginViewModel model)
    {
        ViewData["Title"] = "Đăng nhập";
        ViewData["Description"] = "Đăng nhập tài khoản HV Travel để theo dõi hành trình và quản lý thông tin đặt tour.";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var customers = await _customerRepository.FindAsync(c => c.Email == model.Email.Trim());
        var customer = customers.FirstOrDefault();

        if (customer == null || string.IsNullOrWhiteSpace(customer.PasswordHash) || !BCrypt.Net.BCrypt.Verify(model.Password, customer.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
            return View(model);
        }

        if (!string.Equals(customer.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn hiện không khả dụng. Vui lòng liên hệ HV Travel để được hỗ trợ.");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id ?? string.Empty),
            new(ClaimTypes.Name, customer.Email),
            new(ClaimTypes.Role, "Customer"),
            new("FullName", customer.FullName),
            new("CustomerCode", customer.CustomerCode ?? string.Empty),
            new("EmailVerified", customer.EmailVerified.ToString())
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe
        };

        if (model.RememberMe)
        {
            authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return RedirectToLocal(model.ReturnUrl);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(CustomerRegisterViewModel model)
    {
        ViewData["Title"] = "Đăng ký";
        ViewData["Description"] = "Tạo tài khoản HV Travel để quản lý booking, lưu thông tin liên hệ và nhận ưu đãi du lịch.";

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var normalizedEmail = model.Email.Trim();
        var existingCustomers = await _customerRepository.FindAsync(c => c.Email == normalizedEmail);
        if (existingCustomers.Any())
        {
            ModelState.AddModelError(nameof(model.Email), "Email này đã được sử dụng.");
            return View(model);
        }

        var allCustomers = await _customerRepository.GetAllAsync();
        var now = DateTime.UtcNow;

        var customer = new Customer
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = model.PhoneNumber.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
            CustomerCode = $"CUS{allCustomers.Count() + 1:000000}",
            Address = new Address
            {
                Street = model.Street.Trim(),
                City = model.City.Trim(),
                Country = string.IsNullOrWhiteSpace(model.Country) ? "Việt Nam" : model.Country.Trim()
            },
            Segment = "New",
            Status = "Active",
            EmailVerified = false,
            TokenVersion = 0,
            Stats = new CustomerStats
            {
                LoyaltyPoints = 0,
                LastActivity = now
            },
            CreatedAt = now,
            UpdatedAt = now
        };

        await _customerRepository.AddAsync(customer);
        TempData["AuthSuccessMessage"] = "Tạo tài khoản thành công. Bạn có thể đăng nhập ngay bây giờ.";
        return RedirectToAction(nameof(Login), new { email = normalizedEmail });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
