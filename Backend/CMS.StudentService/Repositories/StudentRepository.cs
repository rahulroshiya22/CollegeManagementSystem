using CMS.StudentService.Data;using CMS.StudentService.Models;
using CMS.StudentService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CMS.StudentService.Repositories
{
    public interface IStudentRepository
    {
        Task<(IEnumerable<Student> Students, int TotalCount)> GetAllAsync(StudentQueryDto query);
        Task<Student?> GetByIdAsync(int id);
        Task<Student?> GetByEmailAsync(string email);
        Task<Student?> GetByRollNumberAsync(string rollNumber);
        Task<IEnumerable<Student>> GetByDepartmentAsync(int departmentId);
        Task<IEnumerable<Student>> GetByAdmissionYearAsync(int year);
        Task<IEnumerable<Student>> GetActiveStudentsAsync();
        Task<Student> AddAsync(Student student);
        Task UpdateAsync(Student student);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class StudentRepository : IStudentRepository
    {
        private readonly StudentDbContext _context;

        public StudentRepository(StudentDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Student> Students, int TotalCount)> GetAllAsync(StudentQueryDto query)
        {
            var queryable = _context.Students.Include(s => s.Department).AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(query.SearchQuery))
            {
                var search = query.SearchQuery.ToLower();
                queryable = queryable.Where(s =>
                    s.FirstName.ToLower().Contains(search) ||
                    s.LastName.ToLower().Contains(search) ||
                    s.Email.ToLower().Contains(search) ||
                    s.RollNumber.ToLower().Contains(search));
            }

            if (query.DepartmentId.HasValue)
                queryable = queryable.Where(s => s.DepartmentId == query.DepartmentId.Value);

            if (query.AdmissionYear.HasValue)
                queryable = queryable.Where(s => s.AdmissionYear == query.AdmissionYear.Value);

            if (!string.IsNullOrWhiteSpace(query.Status))
                queryable = queryable.Where(s => s.Status == query.Status);

            var totalCount = await queryable.CountAsync();

            // Apply pagination
            var students = await queryable
                .OrderBy(s => s.RollNumber)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            return (students, totalCount);
        }

        public async Task<Student?> GetByIdAsync(int id)
        {
            return await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.StudentId == id);
        }

        public async Task<Student?> GetByEmailAsync(string email)
        {
            return await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.Email == email);
        }

        public async Task<Student?> GetByRollNumberAsync(string rollNumber)
        {
            return await _context.Students
                .Include(s => s.Department)
                .FirstOrDefaultAsync(s => s.RollNumber == rollNumber);
        }

        public async Task<IEnumerable<Student>> GetByDepartmentAsync(int departmentId)
        {
            return await _context.Students
                .Include(s => s.Department)
                .Where(s => s.DepartmentId == departmentId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetByAdmissionYearAsync(int year)
        {
            return await _context.Students
                .Include(s => s.Department)
                .Where(s => s.AdmissionYear == year)
                .ToListAsync();
        }

        public async Task<IEnumerable<Student>> GetActiveStudentsAsync()
        {
            return await _context.Students
                .Include(s => s.Department)
                .Where(s => s.Status == "Active")
                .ToListAsync();
        }

        public async Task<Student> AddAsync(Student student)
        {
            _context.Students.Add(student);
            await _context.SaveChangesAsync();
            return student;
        }

        public async Task UpdateAsync(Student student)
        {
            student.UpdatedAt = DateTime.UtcNow;
            _context.Entry(student).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                _context.Students.Remove(student);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Students.AnyAsync(s => s.StudentId == id);
        }
    }
}
