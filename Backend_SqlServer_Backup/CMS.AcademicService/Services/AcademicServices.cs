using CMS.AcademicService.Data;
using CMS.AcademicService.DTOs;
using CMS.AcademicService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AcademicService.Services
{
    // TimeSlot Service
    public interface ITimeSlotService
    {
        Task<IEnumerable<TimeSlot>> GetAllAsync();
        Task<TimeSlot?> GetByIdAsync(int id);
        Task<IEnumerable<TimeSlot>> GetByCourseAsync(int courseId);
        Task<IEnumerable<TimeSlot>> GetByTeacherAsync(int teacherId);
        Task<IEnumerable<TimeSlot>> GetByDayAsync(string dayOfWeek);
        Task<TimeSlot> CreateAsync(CreateTimeSlotDto dto);
        Task<TimeSlot?> UpdateAsync(int id, UpdateTimeSlotDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class TimeSlotService : ITimeSlotService
    {
        private readonly AcademicDbContext _context;
        public TimeSlotService(AcademicDbContext context) => _context = context;

        public async Task<IEnumerable<TimeSlot>> GetAllAsync() =>
            await _context.TimeSlots.Where(t => t.IsActive).ToListAsync();

        public async Task<TimeSlot?> GetByIdAsync(int id) =>
            await _context.TimeSlots.FindAsync(id);

        public async Task<IEnumerable<TimeSlot>> GetByCourseAsync(int courseId) =>
            await _context.TimeSlots.Where(t => t.CourseId == courseId && t.IsActive).ToListAsync();

        public async Task<IEnumerable<TimeSlot>> GetByTeacherAsync(int teacherId) =>
            await _context.TimeSlots.Where(t => t.TeacherId == teacherId && t.IsActive).ToListAsync();

        public async Task<IEnumerable<TimeSlot>> GetByDayAsync(string dayOfWeek) =>
            await _context.TimeSlots.Where(t => t.DayOfWeek == dayOfWeek && t.IsActive).ToListAsync();

        public async Task<TimeSlot> CreateAsync(CreateTimeSlotDto dto)
        {
            var slot = new TimeSlot
            {
                CourseId = dto.CourseId,
                TeacherId = dto.TeacherId,
                DayOfWeek = dto.DayOfWeek,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Room = dto.Room,
                Semester = dto.Semester,
                Year = dto.Year
            };
            _context.TimeSlots.Add(slot);
            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<TimeSlot?> UpdateAsync(int id, UpdateTimeSlotDto dto)
        {
            var slot = await _context.TimeSlots.FindAsync(id);
            if (slot == null) return null;

            if (dto.CourseId.HasValue) slot.CourseId = dto.CourseId.Value;
            if (dto.TeacherId.HasValue) slot.TeacherId = dto.TeacherId.Value;
            if (dto.DayOfWeek != null) slot.DayOfWeek = dto.DayOfWeek;
            if (dto.StartTime.HasValue) slot.StartTime = dto.StartTime.Value;
            if (dto.EndTime.HasValue) slot.EndTime = dto.EndTime.Value;
            if (dto.Room != null) slot.Room = dto.Room;
            if (dto.IsActive.HasValue) slot.IsActive = dto.IsActive.Value;

            await _context.SaveChangesAsync();
            return slot;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var slot = await _context.TimeSlots.FindAsync(id);
            if (slot == null) return false;
            _context.TimeSlots.Remove(slot);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    // Grade Service
    public interface IGradeService
    {
        Task<IEnumerable<Grade>> GetAllAsync();
        Task<Grade?> GetByIdAsync(int id);
        Task<IEnumerable<Grade>> GetByStudentAsync(int studentId);
        Task<IEnumerable<Grade>> GetByCourseAsync(int courseId);
        Task<Grade> CreateAsync(CreateGradeDto dto);
        Task<Grade?> UpdateAsync(int id, UpdateGradeDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class GradeService : IGradeService
    {
        private readonly AcademicDbContext _context;
        public GradeService(AcademicDbContext context) => _context = context;

        public async Task<IEnumerable<Grade>> GetAllAsync() =>
            await _context.Grades.OrderByDescending(g => g.CreatedAt).ToListAsync();

        public async Task<Grade?> GetByIdAsync(int id) =>
            await _context.Grades.FindAsync(id);

        public async Task<IEnumerable<Grade>> GetByStudentAsync(int studentId) =>
            await _context.Grades.Where(g => g.StudentId == studentId).ToListAsync();

        public async Task<IEnumerable<Grade>> GetByCourseAsync(int courseId) =>
            await _context.Grades.Where(g => g.CourseId == courseId).ToListAsync();

        public async Task<Grade> CreateAsync(CreateGradeDto dto)
        {
            var grade = new Grade
            {
                StudentId = dto.StudentId,
                CourseId = dto.CourseId,
                Marks = dto.Marks,
                GradeLetter = dto.GradeLetter,
                Semester = dto.Semester,
                Year = dto.Year,
                Remarks = dto.Remarks
            };
            _context.Grades.Add(grade);
            await _context.SaveChangesAsync();
            return grade;
        }

        public async Task<Grade?> UpdateAsync(int id, UpdateGradeDto dto)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return null;

            if (dto.Marks.HasValue) grade.Marks = dto.Marks.Value;
            if (dto.GradeLetter != null) grade.GradeLetter = dto.GradeLetter;
            if (dto.Remarks != null) grade.Remarks = dto.Remarks;
            grade.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return grade;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade == null) return false;
            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    // Notice Service
    public interface INoticeService
    {
        Task<IEnumerable<Notice>> GetAllAsync();
        Task<IEnumerable<Notice>> GetActiveAsync(string? role = null);
        Task<Notice?> GetByIdAsync(int id);
        Task<Notice> CreateAsync(CreateNoticeDto dto);
        Task<Notice?> UpdateAsync(int id, UpdateNoticeDto dto);
        Task<bool> DeleteAsync(int id);
    }

    public class NoticeService : INoticeService
    {
        private readonly AcademicDbContext _context;
        public NoticeService(AcademicDbContext context) => _context = context;

        public async Task<IEnumerable<Notice>> GetAllAsync() =>
            await _context.Notices.OrderByDescending(n => n.CreatedAt).ToListAsync();

        public async Task<IEnumerable<Notice>> GetActiveAsync(string? role = null)
        {
            var query = _context.Notices.Where(n => n.IsActive);
            if (role != null)
                query = query.Where(n => n.TargetRole == null || n.TargetRole == role);
            return await query.OrderByDescending(n => n.CreatedAt).ToListAsync();
        }

        public async Task<Notice?> GetByIdAsync(int id) =>
            await _context.Notices.FindAsync(id);

        public async Task<Notice> CreateAsync(CreateNoticeDto dto)
        {
            var notice = new Notice
            {
                Title = dto.Title,
                Content = dto.Content,
                Category = dto.Category,
                TargetRole = dto.TargetRole,
                CreatedByUserId = dto.CreatedByUserId,
                CreatedByName = dto.CreatedByName
            };
            _context.Notices.Add(notice);
            await _context.SaveChangesAsync();
            return notice;
        }

        public async Task<Notice?> UpdateAsync(int id, UpdateNoticeDto dto)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null) return null;

            if (dto.Title != null) notice.Title = dto.Title;
            if (dto.Content != null) notice.Content = dto.Content;
            if (dto.Category != null) notice.Category = dto.Category;
            if (dto.TargetRole != null) notice.TargetRole = dto.TargetRole;
            if (dto.IsActive.HasValue) notice.IsActive = dto.IsActive.Value;
            notice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return notice;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var notice = await _context.Notices.FindAsync(id);
            if (notice == null) return false;
            _context.Notices.Remove(notice);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
