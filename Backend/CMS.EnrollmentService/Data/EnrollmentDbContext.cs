using CMS.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.EnrollmentService.Data
{
    public class EnrollmentDbContext : DbContext
    {
        public EnrollmentDbContext(DbContextOptions<EnrollmentDbContext> options) : base(options) { }

        public DbSet<Enrollment> Enrollments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Enrollment>(entity =>
            {
                entity.HasKey(e => e.EnrollmentId);
                entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Year, e.Semester }).IsUnique();
            });
        }
    }
}
