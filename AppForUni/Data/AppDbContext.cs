using Microsoft.EntityFrameworkCore;
using YourApp.Models;

namespace YourApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Employee> Employees => Set<Employee>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Employee>().HasKey(x => x.EmployeeID);
            // Prevent duplicate Employee IDs
            modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeID).IsUnique();
        }
    }
}