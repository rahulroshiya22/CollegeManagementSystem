using CMS.EnrollmentService.Data;
using CMS.EnrollmentService.DTOs;
using CMS.EnrollmentService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.EnrollmentService.Services
{
    public interface IEnrollmentService
    {
        Task<IEnumerable<Enrollment>> GetAllAsync();
        Task<Enrollment?> GetByIdAsync(int id);
        Task<IEnumerable<Enrollment>> GetByStudentAsync(int studentId);
        Task<IEnumerable<Enrollment>> GetByCourseAsync(int courseId);
        Task<Enrollment> CreateAsync(CreateEnrollmentDto dto);
        Task<Enrollment?> UpdateAsync(int id, UpdateEnrollmentDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class EnrollmentManagementService : IEnrollmentService
    {
        private readonly EnrollmentDbContext _context;

        public EnrollmentManagementService(EnrollmentDbContext context) => _context = context;

        public async Task<IEnumerable<Enrollment>> GetAllAsync() =>
            await _context.Enrollments.ToListAsync();

        public async Task<Enrollment?> GetByIdAsync(int id) =>
            await _context.Enrollments.FindAsync(id);

        public async Task<IEnumerable<Enrollment>> GetByStudentAsync(int studentId) =>
            await _context.Enrollments.Where(e => e.StudentId == studentId).ToListAsync();

        public async Task<IEnumerable<Enrollment>> GetByCourseAsync(int courseId) =>
            await _context.Enrollments.Where(e => e.CourseId == courseId).ToListAsync();

        public async Task<Enrollment> CreateAsync(CreateEnrollmentDto dto)
        {
            var enrollment = new Enrollment
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                Semester = dto.Semester,
                Year = dto.Year
            };
            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task<Enrollment?> UpdateAsync(int id, UpdateEnrollmentDto dto)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null) return null;

            if (dto.Status != null) enrollment.Status = dto.Status;
            if (dto.Grade.HasValue) enrollment.Grade = dto.Grade.Value;

            await _context.SaveChangesAsync();
            return enrollment;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null) return false;
            _context.Enrollments.Remove(enrollment);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
