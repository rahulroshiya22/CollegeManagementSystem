using CMS.CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.CourseService.Data
{
    public class CourseDbContext : DbContext
    {
        public CourseDbContext(DbContextOptions<CourseDbContext> options) : base(options) { }

        public DbSet<Course> Courses { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("cms_courses");
            modelBuilder.Entity<Course>(entity =>
            {
                entity.HasKey(e => e.CourseId);
                entity.Property(e => e.CourseCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CourseName).IsRequired().HasMaxLength(200);
                entity.HasIndex(e => e.CourseCode).IsUnique();
                entity.HasOne(e => e.Department).WithMany(d => d.Courses).HasForeignKey(e => e.DepartmentId);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DepartmentId);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, Name = "Computer Science", Code = "CS" },
                new Department { DepartmentId = 2, Name = "Information Technology", Code = "IT" },
                new Department { DepartmentId = 3, Name = "Electronics", Code = "ECE" },
                new Department { DepartmentId = 4, Name = "Mechanical", Code = "ME" },
                new Department { DepartmentId = 5, Name = "Civil", Code = "CE" }
            );
        }
    }
}
