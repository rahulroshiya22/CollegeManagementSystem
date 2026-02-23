using CMS.AttendanceService.Data;
using CMS.AttendanceService.DTOs;
using CMS.AttendanceService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AttendanceService.Services
{
    public interface IAttendanceService
    {
        Task<IEnumerable<Attendance>> GetAllAsync();
        Task<Attendance?> GetByIdAsync(int id);
        Task<IEnumerable<Attendance>> GetByStudentAsync(int studentId);
        Task<IEnumerable<Attendance>> GetByCourseAsync(int courseId);
        Task<Attendance> CreateAsync(CreateAttendanceDto dto);
        Task<Attendance?> UpdateAsync(int id, UpdateAttendanceDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class AttendanceManagementService : IAttendanceService
    {
        private readonly AttendanceDbContext _context;

        public AttendanceManagementService(AttendanceDbContext context) => _context = context;

        public async Task<IEnumerable<Attendance>> GetAllAsync() =>
            await _context.Attendances.ToListAsync();

        public async Task<Attendance?> GetByIdAsync(int id) =>
            await _context.Attendances.FindAsync(id);

        public async Task<IEnumerable<Attendance>> GetByStudentAsync(int studentId) =>
            await _context.Attendances.Where(a => a.StudentId == studentId).ToListAsync();

        public async Task<IEnumerable<Attendance>> GetByCourseAsync(int courseId) =>
            await _context.Attendances.Where(a => a.CourseId == courseId).ToListAsync();

        public async Task<Attendance> CreateAsync(CreateAttendanceDto dto)
        {
            var attendance = new Attendance
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                Date = dto.Date,
                IsPresent = dto.IsPresent,
                Remarks = dto.Remarks
            };
            _context.Attendances.Add(attendance);
            await _context.SaveChangesAsync();
            return attendance;
        }

        public async Task<Attendance?> UpdateAsync(int id, UpdateAttendanceDto dto)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return null;

            attendance.IsPresent = dto.IsPresent;
            attendance.Remarks = dto.Remarks;

            await _context.SaveChangesAsync();
            return attendance;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null) return false;
            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
