using CMS.CourseService.Data;
using CMS.CourseService.DTOs;
using CMS.CourseService.Models;
using CMS.CourseService.Repositories;

namespace CMS.CourseService.Services
{
    public interface ICourseService
    {
        Task<IEnumerable<Course>> GetAllCoursesAsync();
        Task<Course?> GetCourseByIdAsync(int id);
        Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(int departmentId);
        Task<IEnumerable<Course>> GetCoursesBySemesterAsync(int semester);
        Task<Course> CreateCourseAsync(CreateCourseDto dto);
        Task<Course?> UpdateCourseAsync(int id, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int id);
    }

    public class CourseManagementService : ICourseService
    {
        private readonly ICourseRepository _repository;

        public CourseManagementService(ICourseRepository repository) => _repository = repository;

        public async Task<IEnumerable<Course>> GetAllCoursesAsync() =>
            await _repository.GetAllAsync();

        public async Task<Course?> GetCourseByIdAsync(int id) =>
            await _repository.GetByIdAsync(id);

        public async Task<IEnumerable<Course>> GetCoursesByDepartmentAsync(int departmentId) =>
            await _repository.GetByDepartmentAsync(departmentId);

        public async Task<IEnumerable<Course>> GetCoursesBySemesterAsync(int semester) =>
            await _repository.GetBySemesterAsync(semester);

        public async Task<Course> CreateCourseAsync(CreateCourseDto dto)
        {
            var course = new Course
            {
                CourseCode = dto.CourseCode,
                CourseName = dto.CourseName,
                Description = dto.Description,
                Credits = dto.Credits,
                Semester = dto.Semester,
                DepartmentId = dto.DepartmentId
            };
            return await _repository.AddAsync(course);
        }

        public async Task<Course?> UpdateCourseAsync(int id, UpdateCourseDto dto)
        {
            var course = await _repository.GetByIdAsync(id);
            if (course == null) return null;

            if (dto.CourseName != null) course.CourseName = dto.CourseName;
            if (dto.Description != null) course.Description = dto.Description;
            if (dto.Credits.HasValue) course.Credits = dto.Credits.Value;
            if (dto.Semester.HasValue) course.Semester = dto.Semester.Value;
            if (dto.DepartmentId.HasValue) course.DepartmentId = dto.DepartmentId.Value;
            if (dto.IsActive.HasValue) course.IsActive = dto.IsActive.Value;

            await _repository.UpdateAsync(course);
            return course;
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            if (!await _repository.ExistsAsync(id)) return false;
            await _repository.DeleteAsync(id);
            return true;
        }
    }
}
