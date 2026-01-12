using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
            var adminSetting = await _db.AdminSettings.FirstOrDefaultAsync(x => x.Id == 1);

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

            var existing = await _db.AdminSettings.FirstOrDefaultAsync(x => x.Id == 1);
            if (existing == null)
            {
                // Create new with ID = 1
                try
                {
                    // Attempt to force ID 1. Note: If DB ID is IDENTITY, this might be ignored or fail without proper SQL setup.
                    // But we set it here as requested.
                    _db.AdminSettings.Add(new Models.AdminSetting
                    {
                        Id = 1,
                        AdminPassword = YourApp.Utilities.PasswordHelper.ComputeHash(password)
                    });

                    // Optional: If you need to enable IDENTITY_INSERT for SQL Server:
                    // await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT AdminSettings ON");
                    await _db.SaveChangesAsync();
                    // await _db.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT AdminSettings OFF");

                    return Content($"Created new admin password hash with ID 1. You can now login with '{password}'.");
                }
                catch (Exception ex)
                {
                    return Content($"Error seeding: {ex.Message}");
                }
            }
            else
            {
                return Content("Admin password already Set (ID=1 exists). Use UpdatePassword if you are logged in.");
            }
        }

        // --- NEW: Update Password (Must be logged in) ---
        // Usage: /Account/UpdatePassword?newPassword=Secret5678
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UpdatePassword(string newPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword)) return Content("Pass newPassword query param");

            var existing = await _db.AdminSettings.FirstOrDefaultAsync(x => x.Id == 1);
            if (existing != null)
            {
                existing.AdminPassword = YourApp.Utilities.PasswordHelper.ComputeHash(newPassword);
                await _db.SaveChangesAsync();
                return Content($"Success! Admin password updated.");
            }
            return Content("Error: Admin settings not found.");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Select", "Prizes"); // Go back to public page after logout
        }
    }
}