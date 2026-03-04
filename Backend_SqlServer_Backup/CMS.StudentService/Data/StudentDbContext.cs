using CMS.StudentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.StudentService.Data
{
    public class StudentDbContext : DbContext
    {
        public StudentDbContext(DbContextOptions<StudentDbContext> options) : base(options)
        {
        }

        public DbSet<Student> Students { get; set; }
        public DbSet<Department> Departments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Student Configuration
            modelBuilder.Entity<Student>(entity =>
            {
                entity.HasKey(e => e.StudentId);
                entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
                entity.Property(e => e.RollNumber).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.RollNumber).IsUnique();

                entity.HasOne(e => e.Department)
                      .WithMany(d => d.Students)
                      .HasForeignKey(e => e.DepartmentId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Department Configuration
            modelBuilder.Entity<Department>(entity =>
            {
                entity.HasKey(e => e.DepartmentId);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Code).IsRequired().HasMaxLength(10);
                entity.HasIndex(e => e.Code).IsUnique();
            });

            // Seed Data
            modelBuilder.Entity<Department>().HasData(
                new Department { DepartmentId = 1, Name = "Computer Science", Code = "CS", Description = "Computer Science and Engineering" },
                new Department { DepartmentId = 2, Name = "Information Technology", Code = "IT", Description = "Information Technology" },
                new Department { DepartmentId = 3, Name = "Electronics", Code = "ECE", Description = "Electronics and Communication Engineering" },
                new Department { DepartmentId = 4, Name = "Mechanical", Code = "ME", Description = "Mechanical Engineering" },
                new Department { DepartmentId = 5, Name = "Civil", Code = "CE", Description = "Civil Engineering" }
            );
        }
    }
}
