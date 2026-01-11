using Microsoft.EntityFrameworkCore;
using YourApp.Models;

namespace YourApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<PrizeAward> PrizeAwards => Set<PrizeAward>();

        // We still need the Table definition
        public DbSet<AdminSetting> AdminSettings => Set<AdminSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- Employee Configurations ---
            modelBuilder.Entity<Employee>().HasKey(x => x.EmployeeID);
            modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeID).IsUnique();

            // --- PrizeAward Configurations ---
            modelBuilder.Entity<PrizeAward>()
                .HasIndex(x => x.EmployeeID)
                .IsUnique();

            modelBuilder.Entity<PrizeAward>()
                .HasIndex(x => x.PrizeName)
                .IsUnique();

            // --- AdminSetting Configurations ---
            // No HasData() here. The code is now clean.
            // The app will simply read whatever is currently inside the SQL table.
        }
    }
}