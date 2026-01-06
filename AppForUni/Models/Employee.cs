using System.ComponentModel.DataAnnotations;

namespace YourApp.Models
{
    public class Employee
    {
        [Key]
        [StringLength(50)]
        public string EmployeeID { get; set; } = default!;

        [Required, StringLength(200)]
        public string EmployeeName { get; set; } = default!;

        [Required, StringLength(200)]
        public string Department { get; set; } = default!;
    }
}