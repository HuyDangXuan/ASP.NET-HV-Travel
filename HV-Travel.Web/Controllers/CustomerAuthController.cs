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
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Customer"))
        {
            return RedirectToLocal(returnUrl);
        }

        ViewData["Title"] = "Đăng nhập";
        ViewData["Description"] = "Đăng nhập tài khoản HV Travel để theo dõi hành trình và quản lý thông tin đặt tour.";
        return View(new CustomerLoginViewModel { ReturnUrl = returnUrl });
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
            new("FullName", customer.FullName ?? string.Empty),
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
