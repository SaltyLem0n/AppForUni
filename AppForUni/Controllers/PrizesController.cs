using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourApp.Data;
using YourApp.Models;

namespace YourApp.Controllers
{
    public class PrizesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public PrizesController(AppDbContext db, IConfiguration config)
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

        // 1) Select Prize Page
        public IActionResult Select()
        {
            if (IsEventExpired()) return RedirectToAction(nameof(Search));

            var prizes = new List<(string PrizeName, string PrizeAmount)>
            {
                ("รางวัลที่ 1",  "เงินรางวัล 10,000 บาท"),
                ("รางวัลที่ 2",  "เงินรางวัล 8,000 บาท"),
                ("รางวัลที่ 3",  "เงินรางวัล 6,000 บาท"),
                ("รางวัลที่ 4",  "เงินรางวัล 5,000 บาท")
            };
            return View(prizes);
        }


        // --- HELPER: Get winners ONLY for the specific prizeName ---
        private async Task<List<string>> GetWinnerIdsAsync(string prizeName)
        {
            return await _db.PrizeAwards
                .AsNoTracking()
                .Where(x => x.PrizeName == prizeName) // <--- CRITICAL: Filters by current prize only
                .OrderByDescending(x => x.AwardedAt)
                .Select(x => x.EmployeeID)
                .ToListAsync();
        }

        // --- NEW Helper for Search Page (All Prizes: 1st - 4th) ---
        private async Task<List<PrizeAward>> GetAllWinnersAsync()
        {
            return await _db.PrizeAwards
                .AsNoTracking()
                .OrderBy(x => x.PrizeName)          // Group by "รางวัลที่ 1", "2", etc.
                .ThenByDescending(x => x.AwardedAt) // Latest winners on top within group
                .ToListAsync();
        }

        // 2) GET: Announce Page
        [HttpGet]
        public async Task<IActionResult> Announce(string prizeName, string prizeAmount)
        {
            if (IsEventExpired()) return RedirectToAction(nameof(Search));

            if (string.IsNullOrWhiteSpace(prizeName) || string.IsNullOrWhiteSpace(prizeAmount))
                return RedirectToAction(nameof(Select));

            ViewBag.PrizeName = prizeName;
            ViewBag.PrizeAmount = prizeAmount;

            // Fetch list ONLY for this prize
            ViewBag.WinnerIds = await GetWinnerIdsAsync(prizeName);

            return View();
        }

        // 2) POST: Submit Winner
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Announce(string prizeName, string prizeAmount, string scannedCode)
        {
            if (IsEventExpired()) return RedirectToAction(nameof(Search));

            ViewBag.PrizeName = prizeName;
            ViewBag.PrizeAmount = prizeAmount;

            // Always fetch current winners for the table, even if there is an error later
            ViewBag.WinnerIds = await GetWinnerIdsAsync(prizeName);

            scannedCode = (scannedCode ?? "").Trim();

            if (string.IsNullOrWhiteSpace(scannedCode))
            {
                ViewBag.Error = "กรุณาสแกนบาร์โค้ด/กรอก EmployeeID";
                return View();
            }

            var emp = await _db.Employees.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EmployeeID == scannedCode);

            if (emp == null)
            {
                ViewBag.Error = $"ไม่พบพนักงาน EmployeeID: {scannedCode}";
                return View();
            }

            // Transaction
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // -- Logic: Check Limits --
                int limit = 1;
                if (prizeName == "รางวัลที่ 3") limit = 10;
                else if (prizeName == "รางวัลที่ 4") limit = 20;

                // Count current winners for THIS prize
                var currentCount = await _db.PrizeAwards.CountAsync(x => x.PrizeName == prizeName);

                if (currentCount >= limit)
                {
                    ViewBag.Error = $"รางวัลนี้ครบจำนวนแล้ว ({limit} รางวัล): {prizeName}";
                    return View(emp);
                }

                // Check if employee already won ANY prize (Optional: depending on your rules)
                // If they can win multiple DIFFERENT prizes, change this logic. 
                // Currently checks if they won ANYTHING.
                var empAlreadyWon = await _db.PrizeAwards.AnyAsync(x => x.EmployeeID == emp.EmployeeID);
                if (empAlreadyWon)
                {
                    var old = await _db.PrizeAwards.AsNoTracking().FirstAsync(x => x.EmployeeID == emp.EmployeeID);
                    ViewBag.Error = $"พนักงานนี้ได้รับรางวัลแล้ว: {old.PrizeName}";
                    return View(emp);
                }

                // Add Award
                _db.PrizeAwards.Add(new PrizeAward
                {
                    EmployeeID = emp.EmployeeID,
                    PrizeName = prizeName,
                    PrizeAmount = prizeAmount,
                    AwardedAt = DateTime.Now
                });

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // RE-FETCH the list so the new winner appears immediately in the table
                ViewBag.WinnerIds = await GetWinnerIdsAsync(prizeName);

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

        // Search Pages (Keep as is)

        // 3) Search Page (GET)
        [HttpGet]
        public async Task<IActionResult> Search()
        {
            // Fetch ALL winners for the table
            ViewBag.AllWinners = await GetAllWinnersAsync();
            return View();
        }

        // 3) Search Page (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(string employeeId)
        {
            // Fetch ALL winners so table persists after search
            ViewBag.AllWinners = await GetAllWinnersAsync();

            employeeId = (employeeId ?? "").Trim();
            if (string.IsNullOrWhiteSpace(employeeId))
            {
                ViewBag.PopupType = "error";
                ViewBag.PopupTitle = "เกิดข้อผิดพลาด";
                ViewBag.PopupMessage = "กรุณากรอก EmployeeID";
                return View();
            }

            var award = await _db.PrizeAwards.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeID == employeeId);

            if (award != null)
            {
                ViewBag.PopupType = "success";
                ViewBag.PopupTitle = "ขอแสดงความยินดี";
                ViewBag.PopupMessage = $"{award.PrizeName} - {award.PrizeAmount}";
            }
            else
            {
                ViewBag.PopupType = "info";
                ViewBag.PopupTitle = "เสียใจด้วย ปีหน้าเอาใหม่นะ";
                ViewBag.PopupMessage = "";
            }

            // Fetch Employee Name regardless of win/loss if the ID exists (useful for display)
            // Or strictly as per request, just getting it is enough.
            // But specifically for the winner popup which uses ViewBag.EmployeeName:
            var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(x => x.EmployeeID == employeeId);
            if (employee != null)
            {
                ViewBag.EmployeeName = employee.EmployeeName;
            }

            ViewBag.EmployeeId = employeeId;
            return View();
        }
    }
}
