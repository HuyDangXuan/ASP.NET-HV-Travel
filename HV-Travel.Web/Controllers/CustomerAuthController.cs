using System.Security.Claims;
using HVTravel.Application.Interfaces;
using HVTravel.Domain.Entities;
using HVTravel.Domain.Interfaces;
using HVTravel.Web.Models;
using HVTravel.Web.Security;
using HVTravel.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace HVTravel.Web.Controllers;

public class CustomerAuthController : Controller
{
    private const string QrBrowserNonceCookieName = "hvtravel_qr_browser_nonce";
    private readonly IRepository<Customer> _customerRepository;
    private readonly IWebQrLoginService _webQrLoginService;
    private readonly ISearchIndexingService? _searchIndexingService;

    public CustomerAuthController(
        IRepository<Customer> customerRepository,
        IWebQrLoginService webQrLoginService,
        ISearchIndexingService? searchIndexingService = null)
    {
        _customerRepository = customerRepository;
        _webQrLoginService = webQrLoginService;
        _searchIndexingService = searchIndexingService;
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

        var customers = await _customerRepository.FindAsync(customer => customer.Email == model.Email.Trim());
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

        var claims = BuildCustomerClaims(customer);
        var claimsIdentity = new ClaimsIdentity(claims, AuthSchemes.CustomerScheme);
        var authProperties = new AuthenticationProperties { IsPersistent = model.RememberMe };
        if (model.RememberMe)
        {
            authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14);
        }

        await HttpContext.SignInAsync(AuthSchemes.CustomerScheme, new ClaimsPrincipal(claimsIdentity), authProperties);
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
        var existingCustomers = await _customerRepository.FindAsync(customer => customer.Email == normalizedEmail);
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
        await (_searchIndexingService?.UpsertCustomerAsync(customer) ?? Task.CompletedTask);
        TempData["AuthSuccessMessage"] = "Tạo tài khoản thành công. Bạn có thể đăng nhập ngay bây giờ.";
        return RedirectToAction(nameof(Login), new { email = normalizedEmail });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(AuthSchemes.CustomerScheme);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost("CustomerAuth/QrSession")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateQrSession([FromBody] CreateQrLoginSessionRequest? request)
    {
        var nonce = EnsureQrBrowserNonce();
        var response = await _webQrLoginService.CreateSessionAsync(
            request?.ReturnUrl,
            nonce,
            BuildBrowserLabel(Request.Headers.UserAgent.ToString()),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        return Json(response);
    }

    [HttpGet("CustomerAuth/QrSessionStatus")]
    public async Task<IActionResult> GetQrSessionStatus([FromQuery] string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return BadRequest(new { message = "Thiếu sessionId." });
        }

        var response = await _webQrLoginService.GetStatusAsync(sessionId);
        if (response == null)
        {
            return NotFound(new { message = "Không tìm thấy phiên đăng nhập QR." });
        }

        return Json(response);
    }

    [HttpPost("CustomerAuth/QrSessionComplete")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CompleteQrSession([FromBody] CompleteQrLoginRequest? request)
    {
        if (string.IsNullOrWhiteSpace(request?.SessionId))
        {
            return BadRequest(new { message = "Thiếu sessionId." });
        }

        if (!Request.Cookies.TryGetValue(QrBrowserNonceCookieName, out var nonce) || string.IsNullOrWhiteSpace(nonce))
        {
            return BadRequest(new { message = "Trình duyệt hiện tại không có phiên QR hợp lệ." });
        }

        try
        {
            var completion = await _webQrLoginService.CompleteAsync(request.SessionId, nonce);
            var claims = BuildCustomerClaims(completion.Customer);
            var claimsIdentity = new ClaimsIdentity(claims, AuthSchemes.CustomerScheme);
            await HttpContext.SignInAsync(
                AuthSchemes.CustomerScheme,
                new ClaimsPrincipal(claimsIdentity),
                new AuthenticationProperties { IsPersistent = false });

            return Json(new
            {
                status = "authenticated",
                redirectUrl = ResolveRedirectUrl(completion.ReturnUrl)
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private string ResolveRedirectUrl(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return Url.Action("Index", "Home") ?? "/";
    }

    private static List<Claim> BuildCustomerClaims(Customer customer)
    {
        return new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id ?? string.Empty),
            new(ClaimTypes.Name, customer.Email),
            new(ClaimTypes.Email, customer.Email),
            new(ClaimTypes.Role, "Customer"),
            new("FullName", customer.FullName),
            new("PhoneNumber", customer.PhoneNumber ?? string.Empty),
            new("CustomerCode", customer.CustomerCode ?? string.Empty),
            new("EmailVerified", customer.EmailVerified.ToString())
        };
    }

    private string EnsureQrBrowserNonce()
    {
        if (Request.Cookies.TryGetValue(QrBrowserNonceCookieName, out var existingNonce) && !string.IsNullOrWhiteSpace(existingNonce))
        {
            return existingNonce;
        }

        var nonce = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        Response.Cookies.Append(QrBrowserNonceCookieName, nonce, new CookieOptions
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Expires = DateTimeOffset.UtcNow.AddDays(1)
        });

        return nonce;
    }

    private static string BuildBrowserLabel(string userAgent)
    {
        var agent = userAgent ?? string.Empty;
        var browser =
            agent.Contains("Edg/", StringComparison.OrdinalIgnoreCase) ? "Microsoft Edge" :
            agent.Contains("Chrome/", StringComparison.OrdinalIgnoreCase) ? "Google Chrome" :
            agent.Contains("Firefox/", StringComparison.OrdinalIgnoreCase) ? "Mozilla Firefox" :
            agent.Contains("Safari/", StringComparison.OrdinalIgnoreCase) ? "Safari" :
            "Trình duyệt";

        var device =
            agent.Contains("Mobile", StringComparison.OrdinalIgnoreCase) ? "di động" :
            agent.Contains("Tablet", StringComparison.OrdinalIgnoreCase) ? "máy tính bảng" :
            "máy tính";

        return $"{browser} trên {device}";
    }
}
