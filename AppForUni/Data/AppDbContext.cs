using Microsoft.EntityFrameworkCore;
using YourApp.Models;

namespace YourApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<PrizeAward> PrizeAwards => Set<PrizeAward>();
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Employee>().HasKey(x => x.EmployeeID);
            // Prevent duplicate Employee IDs
            modelBuilder.Entity<Employee>().HasIndex(x => x.EmployeeID).IsUnique();


            // กำหนดเงื่อนไขพื้นฐาน (ปรับได้ตามนโยบายจริง)
            // 1 คนรับได้ 1 รางวัล (กันซ้ำ)
            modelBuilder.Entity<PrizeAward>()
                .HasIndex(x => x.EmployeeID)
                .IsUnique();

            // 1 รางวัลมีผู้ชนะได้ 1 คน (กันซ้ำ)
            modelBuilder.Entity<PrizeAward>()
                .HasIndex(x => x.PrizeName)
                .IsUnique();


        }
    }
}