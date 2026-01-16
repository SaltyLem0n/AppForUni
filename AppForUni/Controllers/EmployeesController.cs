using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing;
using System.Drawing.Imaging;
using YourApp.Data;
using YourApp.Models;
using YourApp.ViewModels;
using ZXing;
using ZXing.Common;

namespace YourApp.Controllers
{
    [Authorize] // <--- This forces Login ONLY when accessing this controller
    public class EmployeesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public EmployeesController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // Helper to check if event is expired
        private bool IsEventExpired()
        {
            var endDateStr = _config["EventSettings:EventEndDate"];
            if (DateTime.TryParse(endDateStr, out var endDate))
            {
                return DateTime.Now > endDate;
            }
            return false;
        }

        // Page 1: Display list + Upload form
        public async Task<IActionResult> Index()
        {
            // If expired, redirect to Prize Search
            if (IsEventExpired()) return RedirectToAction("Search", "Prizes");

            var employees = await _db.Employees
                .AsNoTracking()
                .OrderBy(x => x.ExcelRowOrder)
                .ToListAsync();
            return View(employees);
        }

        // Import Excel -> Upsert DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportExcel(IFormFile excelFile)
        {
            if (IsEventExpired()) return RedirectToAction("Search", "Prizes");

            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Msg"] = "Please select an Excel file (.xlsx)";
                return RedirectToAction(nameof(Index));
            }

            // ... (File validation checks can go here) ...

            int added = 0, updated = 0, skipped = 0;
            try
            {
                using var stream = excelFile.OpenReadStream();
                using var wb = new XLWorkbook(stream);
                var ws = wb.Worksheets.First();
                var lastRow = ws.LastRowUsed()?.RowNumber() ?? 0;

                if (lastRow < 2)
                {
                    TempData["Msg"] = "No data found (needs at least 1 data row)";
                    return RedirectToAction(nameof(Index));
                }

                using var tx = await _db.Database.BeginTransactionAsync();

                // Clear existing data before import (PrizeAwards first due to FK)
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM PrizeAwards");
                await _db.Database.ExecuteSqlRawAsync("DELETE FROM Employees");

                for (int r = 2; r <= lastRow; r++)
                {
                    // Read columns 1, 2, 3 (ID, Name, Department)
                    var id = ws.Cell(r, 1).GetString().Trim();
                    var name = ws.Cell(r, 2).GetString().Trim();
                    var dep = ws.Cell(r, 3).GetString().Trim();

                    if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    {
                        skipped++; continue;
                    }

                    var existing = await _db.Employees.FindAsync(id);
                    if (existing == null)
                    {
                        _db.Employees.Add(new Employee
                        {
                            EmployeeID = id,
                            EmployeeName = name,
                            Department = dep,
                            ExcelRowOrder = r  // Save Excel row order
                        });
                        added++;
                    }
                    else
                    {
                        existing.EmployeeName = name;
                        existing.Department = dep;
                        existing.ExcelRowOrder = r;  // Update Excel row order
                        updated++;
                    }
                }
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                TempData["Msg"] = $"Import Complete: Added {added}, Updated {updated}, Skipped {skipped}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Msg"] = $"Error: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // Page 2: Generate Coupons (Barcode)
        public async Task<IActionResult> Coupons()
        {
            var employees = await _db.Employees
                .AsNoTracking()
                .OrderBy(x => x.ExcelRowOrder)
                .ToListAsync();

            var model = new List<EmployeeCouponVM>(employees.Count);
            foreach (var e in employees)
            {
                // Using BarcodeFormat.CODE_128 as per version 2 requirements
                var barcodeUri = GenerateBarcodeDataUri(e.EmployeeID, BarcodeFormat.CODE_128);
                model.Add(new EmployeeCouponVM
                {
                    EmployeeID = e.EmployeeID,
                    EmployeeName = e.EmployeeName,
                    Department = e.Department,
                    BarcodeDataUri = barcodeUri
                });
            }
            return View(model);
        }

        private static string GenerateBarcodeDataUri(string text, BarcodeFormat format)
        {
            var writer = new BarcodeWriterPixelData
            {
                Format = format,
                Options = new EncodingOptions { Height = 120, Width = 520, Margin = 0, PureBarcode = true }
            };

            var pixelData = writer.Write(text);
            using var bmp = new Bitmap(pixelData.Width, pixelData.Height, PixelFormat.Format32bppArgb);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bmpData.Scan0, pixelData.Pixels.Length);
            }
            finally { bmp.UnlockBits(bmpData); }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }
    }
}