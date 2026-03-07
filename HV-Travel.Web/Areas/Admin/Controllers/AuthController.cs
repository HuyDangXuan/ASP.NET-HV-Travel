using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using HVTravel.Application.Interfaces;

namespace HVTravel.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    // [Route("Admin/[controller]")] // Area routing handles this now, or keep for explicit
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IEmailService _emailService;

        public AuthController(IAuthService authService, IEmailService emailService)
        {
            _authService = authService;
            _emailService = emailService;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Dashboard");
            }
            return View();
        }

        [HttpGet("ForgotPassword")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ViewBag.Error = "Vui lòng nhập địa chỉ email.";
                return View();
            }

            var exists = await _authService.CheckEmailExistsAsync(email);
            if (!exists)
            {
                ViewBag.Error = "Không tìm thấy tài khoản với email này.";
                return View();
            }

            // Generate OTP
            var otp = new Random().Next(100000, 999999).ToString();
            
            // Store Email and OTP in TempData to verify in the next step
            TempData["ResetEmail"] = email;
            TempData["OTP"] = otp;
            TempData["OTPExpiry"] = DateTime.UtcNow.AddMinutes(5);

            var subject = "Mã OTP Khôi phục mật khẩu - HV Travel";
            var body = $@"
                <h3>Yêu cầu khôi phục mật khẩu</h3>
                <p>Xin chào,</p>
                <p>Chúng tôi nhận được yêu cầu khôi phục mật khẩu cho tài khoản liên kết với email này.</p>
                <p>Mã OTP của bạn là: <strong><span style='font-size: 24px; color: #0ea5e9;'>{otp}</span></strong></p>
                <p>Mã này sẽ hết hạn sau 5 phút.</p>
                <p>Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
                <br>
                <p>Trân trọng,</p>
                <p>Đội ngũ HV Travel</p>
            ";

            try
            {
                await _emailService.SendEmailAsync(email, subject, body);
                // Redirect to OTP verification page
                return RedirectToAction("VerifyOTP");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Có lỗi xảy ra khi gửi email: " + ex.Message;
            }

            return View();
        }

        [HttpGet("VerifyOTP")]
        public IActionResult VerifyOTP()
        {
            if (TempData["ResetEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            TempData.Keep("ResetEmail");
            TempData.Keep("OTP");
            TempData.Keep("OTPExpiry");
            return View();
        }

        [HttpPost("VerifyOTP")]
        public IActionResult VerifyOTP(string otp1, string otp2, string otp3, string otp4, string otp5, string otp6)
        {
            var email = TempData["ResetEmail"]?.ToString();
            var savedOtp = TempData["OTP"]?.ToString();
            var expiryObj = TempData["OTPExpiry"];
            
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(savedOtp) || expiryObj == null)
            {
                return RedirectToAction("ForgotPassword");
            }

            var expiry = (DateTime)expiryObj;
            if (DateTime.UtcNow > expiry)
            {
                ViewBag.Error = "Mã OTP đã hết hạn. Vui lòng thử lại.";
                TempData.Remove("ResetEmail");
                TempData.Remove("OTP");
                TempData.Remove("OTPExpiry");
                return RedirectToAction("ForgotPassword");
            }

            var inputOtp = $"{otp1}{otp2}{otp3}{otp4}{otp5}{otp6}";

            if (inputOtp == savedOtp)
            {
                // OTP is correct
                TempData.Keep("ResetEmail");
                // Normally you would set a token here and redirect to Reset Password view
                // For now, redirect to a hypothetical ResetPassword action
                TempData["OTPVerified"] = true;
                return RedirectToAction("ResetPassword");
            }

            ViewBag.Error = "Mã OTP không chính xác.";
            TempData.Keep("ResetEmail");
            TempData.Keep("OTP");
            TempData.Keep("OTPExpiry");
            return View();
        }

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword()
        {
            if (TempData["ResetEmail"] == null || TempData["OTPVerified"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            TempData.Keep("ResetEmail"); // Keep for POST step
            return View(new { Email = TempData["ResetEmail"].ToString() });
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(string email, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
            {
                ViewBag.Error = "Vui lòng nhập đầy đủ thông tin.";
                return View(new { Email = email });
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "Mật khẩu xác nhận không khớp.";
                return View(new { Email = email });
            }

            var success = await _authService.UpdatePasswordAsync(email, newPassword);
            
            if (success)
            {
                // Clear temp data
                TempData.Remove("ResetEmail");
                TempData.Remove("OTPVerified");
                
                // Show success message and redirect
                // Usually done via TempData for the next request
                TempData["SuccessMessage"] = "Mật khẩu của bạn đã được đặt lại thành công. Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Có lỗi xảy ra khi cập nhật mật khẩu. Vui lòng thử lại.";
            return View(new { Email = email });
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(string email, string password, string fullName)
        {
            if (ModelState.IsValid)
            {
                var user = new Domain.Entities.User
                {
                    Email = email,
                    PasswordHash = password, // Service handles hashing
                    FullName = fullName,
                    Role = "client"
                };

                try
                {
                    await _authService.RegisterAsync(user);
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    ViewBag.Error = ex.Message;
                }
            }
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe = false, string returnUrl = null)
        {
            var user = await _authService.ValidateUserAsync(email, password);
            if (user != null)
            {
                // Kiểm tra tài khoản bị vô hiệu hóa
                if (user.Status == "Inactive")
                {
                    return RedirectToAction("AccountDeactivated");
                }
                
                if (user.Role == "client")
                {
                   return RedirectToAction("AccountPending");
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("FullName", user.FullName ?? "")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = rememberMe
                };
                
                if (rememberMe)
                {
                    authProperties.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
                }

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.Error = "Email hoặc mật khẩu không chính xác";
            return View();
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }
        
        [HttpGet("AccountPending")]
        public IActionResult AccountPending()
        {
            return View();
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }
        
        [HttpGet("AccountDeactivated")]
        public IActionResult AccountDeactivated()
        {
            return View();
        }
    }
}
