using CMS.CourseService.Data;
using CMS.CourseService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.CourseService.Repositories
{
    public interface ICourseRepository
    {
        Task<IEnumerable<Course>> GetAllAsync();
        Task<Course?> GetByIdAsync(int id);
        Task<IEnumerable<Course>> GetByDepartmentAsync(int departmentId);
        Task<IEnumerable<Course>> GetBySemesterAsync(int semester);
        Task<Course> AddAsync(Course course);
        Task UpdateAsync(Course course);
        Task DeleteAsync(int id);
        Task<bool> ExistsAsync(int id);
    }

    public class CourseRepository : ICourseRepository
    {
        private readonly CourseDbContext _context;
        public CourseRepository(CourseDbContext context) => _context = context;

        public async Task<IEnumerable<Course>> GetAllAsync() =>
            await _context.Courses.Include(c => c.Department).ToListAsync();

        public async Task<Course?> GetByIdAsync(int id) =>
            await _context.Courses.Include(c => c.Department).FirstOrDefaultAsync(c => c.CourseId == id);

        public async Task<IEnumerable<Course>> GetByDepartmentAsync(int departmentId) =>
            await _context.Courses.Include(c => c.Department).Where(c => c.DepartmentId == departmentId).ToListAsync();

        public async Task<IEnumerable<Course>> GetBySemesterAsync(int semester) =>
            await _context.Courses.Include(c => c.Department).Where(c => c.Semester == semester).ToListAsync();

        public async Task<Course> AddAsync(Course course)
        {
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();
            return course;
        }

        public async Task UpdateAsync(Course course)
        {
            _context.Entry(course).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course != null)
            {
                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(int id) =>
            await _context.Courses.AnyAsync(c => c.CourseId == id);
    }
}
