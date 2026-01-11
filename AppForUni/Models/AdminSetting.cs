using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class AdminSetting
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AdminPassword { get; set; } = default!;
    }
}