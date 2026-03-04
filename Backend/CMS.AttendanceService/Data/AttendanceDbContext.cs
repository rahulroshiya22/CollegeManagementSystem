using CMS.AttendanceService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AttendanceService.Data
{
    public class AttendanceDbContext : DbContext
    {
        public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options) : base(options) { }
        public DbSet<Attendance> Attendances { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("cms_attendance");
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.HasKey(e => e.AttendanceId);
                entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Date }).IsUnique();
            });
        }
    }
}
