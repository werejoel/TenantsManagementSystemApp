using TenantsManagementApp.Services;
using TenantsManagementApp.ViewModels;
using TenantsManagementApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace TenantsManagementApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _accountService = accountService;
            _logger = logger;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var result = await _accountService.RegisterUserAsync(model);

                if (result.Succeeded)
                {
                    // Store email in TempData to pass to confirmation page
                    TempData["Email"] = model.Email;

                    // Check if email was sent (IdentityResult.Success means user created, but email may have failed)
                    if (result == IdentityResult.Success)
                    {
                        TempData["EmailError"] = "Registration succeeded, but confirmation email could not be sent. Please contact support.";
                    }
                    return RedirectToAction("ConfirmEmailCode");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for email: {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        // GET: /Account/RegistrationConfirmation
        [HttpGet]
        public IActionResult RegistrationConfirmation()
        {
            return View();
        }

        // GET: /Account/ConfirmEmailCode
        [HttpGet]
        public async Task<IActionResult> ConfirmEmailCode()
        {
            var model = new ConfirmEmailCodeViewModel();
            // If email is passed via TempData, prefill it
            if (TempData.ContainsKey("Email"))
                model.Email = TempData["Email"]?.ToString() ?? string.Empty;

            // Try to get user and set code expiration and resend wait
            if (!string.IsNullOrEmpty(model.Email))
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user != null)
                {
                    if (user.EmailConfirmationCodeExpiresOn.HasValue)
                        TempData["CodeExpiresOn"] = user.EmailConfirmationCodeExpiresOn.Value.ToLocalTime().ToString("HH:mm:ss");
                    if (user.LastConfirmationCodeResendOn.HasValue)
                    {
                        var secondsLeft = 120 - (int)(DateTime.UtcNow - user.LastConfirmationCodeResendOn.Value).TotalSeconds;
                        if (secondsLeft > 0)
                            TempData["ResendWait"] = secondsLeft;
                    }
                }
            }
            return View(model);
        }

        // POST: /Account/ConfirmEmailCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmailCode(ConfirmEmailCodeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            // Check if code is expired
            if (user.EmailConfirmationCodeExpiresOn.HasValue &&
                user.EmailConfirmationCodeExpiresOn.Value < DateTime.UtcNow)
            {
                ModelState.AddModelError("", "Confirmation code has expired. Please request a new one.");
                return View(model);
            }

            // Use the AccountService method to confirm email with code
            var result = await _accountService.ConfirmEmailWithCodeAsync(user.Id, model.Code);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Email confirmed successfully!";
                return RedirectToAction("Login");
            }
            else
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
                return View(model);
            }
        }

        // POST: /Account/ResendEmailCode
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailCode(ConfirmEmailCodeViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Email))
            {
                TempData["ResendError"] = "Please enter your email to resend the code.";
                return RedirectToAction("ConfirmEmailCode");
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                TempData["ResendError"] = "User not found.";
                return RedirectToAction("ConfirmEmailCode");
            }

            // Limit resend attempts to once every 2 minutes
            if (user.LastConfirmationCodeResendOn.HasValue && (DateTime.UtcNow - user.LastConfirmationCodeResendOn.Value).TotalSeconds < 120)
            {
                TempData["ResendError"] = "You can request a new code every 2 minutes. Please wait before trying again.";
                return RedirectToAction("ConfirmEmailCode");
            }

            try
            {
                // Use the AccountService method to resend confirmation code
                var success = await _accountService.ResendConfirmationCodeAsync(model.Email);

                if (success)
                {
                    TempData["ResendSuccess"] = "A new confirmation code has been sent to your email.";
                }
                else
                {
                    TempData["ResendError"] = "Failed to send confirmation code. Please try again later.";
                }
            }
            catch
            {
                TempData["ResendError"] = "Failed to send confirmation code. Please try again later.";
            }

            return RedirectToAction("ConfirmEmailCode");
        }

        // GET: /Account/ConfirmEmail
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
                    return BadRequest("Invalid email confirmation request.");

                var result = await _accountService.ConfirmEmailAsync(userId, token);

                if (result.Succeeded)
                    return View("EmailConfirmed");

                // Combine errors into one message or pass errors to the view
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View("Error");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming email for UserId: {UserId}", userId);
                ModelState.AddModelError("", "An unexpected error occurred during email confirmation.");
                return View("Error");
            }
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login(string? ReturnUrl = null)
        {
            ViewData["ReturnUrl"] = ReturnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var result = await _accountService.LoginUserAsync(model);

                if (result.Succeeded)
                {
                    // Redirect back to original page if ReturnUrl exists and is local
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);

                    // Otherwise, redirect to a default page (like user profile)
                    return RedirectToAction("Profile", "Account");
                }

                // Handle login failure (e.g., invalid credentials or unconfirmed email)
                if (result.IsNotAllowed)
                    ModelState.AddModelError("", "Email is not confirmed yet.");
                else
                    ModelState.AddModelError("", "Invalid login attempt.");

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for email: {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        //HttpGet
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Account");

            try
            {
                var model = await _accountService.GetUserProfileByEmailAsync(email);
                return View(model);
            }
            catch (ArgumentException)
            {
                return View("Error");
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _accountService.LogoutUserAsync();
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                // Optionally redirect to error page or home with message
                return RedirectToAction("Index", "Home");
            }
        }

        // GET: /Account/ResendEmailConfirmation
        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        // POST: /Account/ResendEmailConfirmation
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(ResendConfirmationEmailViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                await _accountService.SendEmailConfirmationAsync(model.Email);
                TempData["ResendSuccess"] = "A new confirmation code has been sent to your email.";
                return View("ResendEmailConfirmationSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email confirmation to: {Email}", model.Email);
                ModelState.AddModelError("", "An unexpected error occurred. Please try again later.");
                return View(model);
            }
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult AccessDenied(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }
    }
}