using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YourApp.Models
{
    public class PrizeAward
    {
        [Key]
        public int PrizeAwardId { get; set; }

        [Required, StringLength(50)]
        public string EmployeeID { get; set; } = default!;

        [Required, StringLength(100)]
        public string PrizeName { get; set; } = default!;   // เช่น "รางวัลที่ 1"

        [Required, StringLength(100)]
        public string PrizeAmount { get; set; } = default!; // เช่น "เงินรางวัล 10,000 บาท"

        public DateTime AwardedAt { get; set; } = DateTime.Now;

        // optional FK navigation
        [ForeignKey(nameof(EmployeeID))]
        public Employee? Employee { get; set; }
    }
}
