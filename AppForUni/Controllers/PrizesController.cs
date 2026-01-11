using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Controllers
{
    public class PrizesController : Controller
    {
        private readonly AppDbContext _db;
        public PrizesController(AppDbContext db) => _db = db;

        // 1) หน้าเลือกรางวัล 10 ปุ่ม
        public IActionResult Select()
        {
            // กำหนดชุดรางวัลตัวอย่าง (ปรับข้อความ/จำนวนเงินได้ตามจริง)
            var prizes = new List<(string PrizeName, string PrizeAmount)>
            {
                ("รางวัลที่ 1",  "เงินรางวัล 10,000 บาท"),
                ("รางวัลที่ 2",  "เงินรางวัล 8,000 บาท"),
                ("รางวัลที่ 3",  "เงินรางวัล 6,000 บาท"),
                ("รางวัลที่ 4",  "เงินรางวัล 5,000 บาท")
            };

            return View(prizes);
        }

        // 2) GET หน้า “ประกาศรางวัล”
        [HttpGet]
        public IActionResult Announce(string prizeName, string prizeAmount)
        {
            if (string.IsNullOrWhiteSpace(prizeName) || string.IsNullOrWhiteSpace(prizeAmount))
                return RedirectToAction(nameof(Select));

            ViewBag.PrizeName = prizeName;
            ViewBag.PrizeAmount = prizeAmount;
            return View();
        }

        //
        // 2) POST: รับค่าบาร์โค้ด(=EmployeeID) -> lookup employee -> บันทึกรางวัลใน Transaction เดียว
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Announce(string prizeName, string prizeAmount, string scannedCode)
        {
            ViewBag.PrizeName = prizeName;
            ViewBag.PrizeAmount = prizeAmount;

            scannedCode = (scannedCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(scannedCode))
            {
                ViewBag.Error = "กรุณาสแกนบาร์โค้ด/กรอก EmployeeID";
                return View();
            }

            // 1) หา employee
            var emp = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeID == scannedCode);

            if (emp == null)
            {
                ViewBag.Error = $"ไม่พบพนักงาน EmployeeID: {scannedCode}";
                return View();
            }

            // 2) บันทึกรางวัลแบบ atomic (Transaction)
            using var tx = await _db.Database.BeginTransactionAsync();

            try
            {
                // --- NEW LOGIC START: Check Limits ---

                // Determine the limit based on the prize name
                int limit = 1; // Default for 1st and 2nd prizes
                if (prizeName == "รางวัลที่ 3")
                {
                    limit = 10;
                }
                else if (prizeName == "รางวัลที่ 4")
                {
                    limit = 20;
                }

                // Count how many times this prize has already been awarded
                var currentCount = await _db.PrizeAwards.CountAsync(x => x.PrizeName == prizeName);

                if (currentCount >= limit)
                {
                    ViewBag.Error = $"รางวัลนี้ครบจำนวนแล้ว ({limit} รางวัล): {prizeName}";
                    return View(emp);
                }
                // --- NEW LOGIC END ---


                // กันกรณีพนักงานคนนี้ได้รางวัลแล้ว (Existing logic)
                var empAlreadyWon = await _db.PrizeAwards.AnyAsync(x => x.EmployeeID == emp.EmployeeID);
                if (empAlreadyWon)
                {
                    var old = await _db.PrizeAwards.AsNoTracking()
                        .FirstAsync(x => x.EmployeeID == emp.EmployeeID);

                    ViewBag.Error = $"พนักงานนี้ได้รับรางวัลแล้ว: {old.PrizeName} ({old.PrizeAmount})";
                    return View(emp);
                }

                if (_db.PrizeAwards is null)
                {
                    throw new InvalidOperationException("Database context is not initialized.");
                }

                _db.PrizeAwards.Add(new PrizeAward
                {
                    EmployeeID = emp.EmployeeID,
                    PrizeName = prizeName,
                    PrizeAmount = prizeAmount,
                    AwardedAt = DateTime.Now
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // Optional: Show how many are left in the success message
                ViewBag.Success = $"บันทึกผลรางวัลเรียบร้อย (มอบแล้ว {currentCount + 1}/{limit})";
                return View(emp);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                ViewBag.Error = "บันทึกล้มเหลว: " + ex.Message;
                return View(emp);
            }
        }

        // 3) หน้า Search (GET)
        [HttpGet]
        public IActionResult Search()
        {
            return View();
        }

        // 3) หน้า Search (POST): ค้นหา EmployeeID แล้วให้ popup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string employeeId)
        {
            employeeId = (employeeId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(employeeId))
            {
                ViewBag.PopupType = "error";
                ViewBag.PopupTitle = "Error";
                ViewBag.PopupMessage = "กรุณากรอก EmployeeID";
                return View();
            }

            var award = await _db.PrizeAwards.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

            if (award != null)
            {
                ViewBag.PopupType = "success";
                ViewBag.PopupTitle = "Congratulation";
                ViewBag.PopupMessage = $"{award.PrizeName} - {award.PrizeAmount}";
            }
            else
            {
                ViewBag.PopupType = "info";
                ViewBag.PopupTitle = "Sorry, see you next year 2027";
                ViewBag.PopupMessage = "";
            }

            ViewBag.EmployeeId = employeeId;
            return View();
        }
    }
}
