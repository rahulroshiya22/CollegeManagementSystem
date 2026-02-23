using CMS.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Teacher> Teachers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.GoogleId);
            
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.GoogleId).HasMaxLength(255);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500);
            entity.Property(e => e.PhotoUrl).HasMaxLength(500);
            entity.Property(e => e.RefreshToken).HasMaxLength(500);
        });

        // Teacher configuration
        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId);
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Specialization).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Qualification).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
        });

        var dt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var teacherPwd = BCrypt.Net.BCrypt.HashPassword("Teacher@123", 12);
        var studentPwd = BCrypt.Net.BCrypt.HashPassword("Student@123", 12);

        // Seed admin + 10 teachers + 5 students
        modelBuilder.Entity<User>().HasData(
            new User { UserId = 1, Email = "admin@cms.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123", 12), FirstName = "System", LastName = "Administrator", Role = UserRole.Admin, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt },
            new User { UserId = 2, Email = "rajesh.sharma@cms.com", PasswordHash = teacherPwd, FirstName = "Rajesh", LastName = "Sharma", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 1, CreatedAt = dt },
            new User { UserId = 3, Email = "priya.patel@cms.com", PasswordHash = teacherPwd, FirstName = "Priya", LastName = "Patel", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 2, CreatedAt = dt },
            new User { UserId = 4, Email = "sunil.verma@cms.com", PasswordHash = teacherPwd, FirstName = "Sunil", LastName = "Verma", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 3, CreatedAt = dt },
            new User { UserId = 5, Email = "kavita.joshi@cms.com", PasswordHash = teacherPwd, FirstName = "Kavita", LastName = "Joshi", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 4, CreatedAt = dt },
            new User { UserId = 6, Email = "ramesh.iyer@cms.com", PasswordHash = teacherPwd, FirstName = "Ramesh", LastName = "Iyer", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 5, CreatedAt = dt },
            new User { UserId = 7, Email = "deepa.nair@cms.com", PasswordHash = teacherPwd, FirstName = "Deepa", LastName = "Nair", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 6, CreatedAt = dt },
            new User { UserId = 8, Email = "anil.kumar@cms.com", PasswordHash = teacherPwd, FirstName = "Anil", LastName = "Kumar", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 7, CreatedAt = dt },
            new User { UserId = 9, Email = "meena.rao@cms.com", PasswordHash = teacherPwd, FirstName = "Meena", LastName = "Rao", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 8, CreatedAt = dt },
            new User { UserId = 10, Email = "vikram.singh@cms.com", PasswordHash = teacherPwd, FirstName = "Vikram", LastName = "Singh", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 9, CreatedAt = dt },
            new User { UserId = 11, Email = "anjali.mishra@cms.com", PasswordHash = teacherPwd, FirstName = "Anjali", LastName = "Mishra", Role = UserRole.Teacher, IsActive = true, AuthProvider = AuthProvider.Local, TeacherId = 10, CreatedAt = dt },
            new User { UserId = 12, Email = "student1@cms.com", PasswordHash = studentPwd, FirstName = "Aarav", LastName = "Sharma", Role = UserRole.Student, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt },
            new User { UserId = 13, Email = "student2@cms.com", PasswordHash = studentPwd, FirstName = "Vivaan", LastName = "Patel", Role = UserRole.Student, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt },
            new User { UserId = 14, Email = "student3@cms.com", PasswordHash = studentPwd, FirstName = "Diya", LastName = "Gupta", Role = UserRole.Student, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt },
            new User { UserId = 15, Email = "student4@cms.com", PasswordHash = studentPwd, FirstName = "Ananya", LastName = "Iyer", Role = UserRole.Student, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt },
            new User { UserId = 16, Email = "student5@cms.com", PasswordHash = studentPwd, FirstName = "Ishaan", LastName = "Joshi", Role = UserRole.Student, IsActive = true, AuthProvider = AuthProvider.Local, CreatedAt = dt }
        );

        // Seed 10 teachers
        modelBuilder.Entity<Teacher>().HasData(
            new Teacher { TeacherId = 1, UserId = 2, Department = "Computer Science", Specialization = "Data Structures & Algorithms", Qualification = "M.Tech in CS", Experience = 8, PhoneNumber = "9876543210", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 2, UserId = 3, Department = "Computer Science", Specialization = "Machine Learning & AI", Qualification = "Ph.D in CS", Experience = 12, PhoneNumber = "9876543211", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 3, UserId = 4, Department = "Information Technology", Specialization = "Web Development & Cloud", Qualification = "M.Tech in IT", Experience = 6, PhoneNumber = "9876543212", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 4, UserId = 5, Department = "Information Technology", Specialization = "Cyber Security", Qualification = "Ph.D in Information Security", Experience = 10, PhoneNumber = "9876543213", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 5, UserId = 6, Department = "Electronics", Specialization = "VLSI Design", Qualification = "M.Tech in ECE", Experience = 9, PhoneNumber = "9876543214", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 6, UserId = 7, Department = "Mechanical", Specialization = "Thermodynamics & Fluid Mechanics", Qualification = "Ph.D in ME", Experience = 15, PhoneNumber = "9876543215", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 7, UserId = 8, Department = "Civil", Specialization = "Structural Engineering", Qualification = "M.Tech in Structural", Experience = 7, PhoneNumber = "9876543216", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 8, UserId = 9, Department = "Electrical", Specialization = "Power Systems", Qualification = "M.Tech in EE", Experience = 11, PhoneNumber = "9876543217", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 9, UserId = 10, Department = "Mathematics", Specialization = "Applied Mathematics", Qualification = "Ph.D in Mathematics", Experience = 14, PhoneNumber = "9876543218", IsActive = true, JoiningDate = dt, CreatedAt = dt },
            new Teacher { TeacherId = 10, UserId = 11, Department = "Physics", Specialization = "Quantum Mechanics", Qualification = "Ph.D in Physics", Experience = 13, PhoneNumber = "9876543219", IsActive = true, JoiningDate = dt, CreatedAt = dt }
        );
    }
}
