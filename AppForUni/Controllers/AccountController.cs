using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using YourApp.Data;
using YourApp.Utilities;

namespace YourApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _db;

        public AccountController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Keep track of where the user wanted to go (e.g., /Employees)
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string password, string? returnUrl = null)
        {
            var adminSetting = await _db.AdminSettings.FirstOrDefaultAsync();

            if (adminSetting != null && PasswordHelper.ComputeHash(password) == adminSetting.AdminPassword)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "Admin"),
                    new Claim(ClaimTypes.Role, "Admin")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    new AuthenticationProperties { IsPersistent = true });

                // --- REDIRECT LOGIC ---
                // If the user came from "Employees", send them back there.
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // Default fallback
                return RedirectToAction("Index", "Employees");
            }

            ViewBag.Error = "Incorrect Passcode";
            ViewData["ReturnUrl"] = returnUrl; // Keep the return URL even if they fail
            return View();
        }

        // --- NEW: One-time Seed or Manual Fix ---
        // Usage: /Account/SeedAdmin?password=Secret1234
        [HttpGet]
        public async Task<IActionResult> SeedAdmin(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return Content("Pass password query param");

            var existing = await _db.AdminSettings.FirstOrDefaultAsync();
            if (existing == null)
            {
                // Create new
                _db.AdminSettings.Add(new Models.AdminSetting
                {
                    AdminPassword = YourApp.Utilities.PasswordHelper.ComputeHash(password)
                });
                await _db.SaveChangesAsync();
                return Content($"Created new admin password hash. You can now login with '{password}'.");
            }
            else
            {
                // Optional: Allow overwrite if you really want to reset it easily during dev
                // existing.AdminPassword = YourApp.Utilities.PasswordHelper.ComputeHash(password);
                // await _db.SaveChangesAsync();
                // return Content($"Updated admin password hash. You can now login with '{password}'.");

                return Content("Admin password already Set. Use UpdatePassword if you are logged in, or direct SQL if locked out.");
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Select", "Prizes"); // Go back to public page after logout
        }
    }
}