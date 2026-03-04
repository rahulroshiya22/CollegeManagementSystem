using CMS.AuthService.Data;
using CMS.AuthService.DTOs;
using CMS.AuthService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AuthService.Services;

public interface ITeacherService
{
    Task<(IEnumerable<TeacherResponseDto> Teachers, int TotalCount)> GetAllTeachersAsync(TeacherQueryDto query);
    Task<TeacherResponseDto?> GetTeacherByIdAsync(int id);
    Task<TeacherResponseDto?> GetTeacherByUserIdAsync(int userId);
    Task<TeacherResponseDto?> CreateTeacherAsync(CreateTeacherDto dto);
    Task<TeacherResponseDto?> UpdateTeacherAsync(int id, UpdateTeacherDto dto);
    Task<bool> DeleteTeacherAsync(int id);
    Task<IEnumerable<TeacherResponseDto>> GetTeachersByDepartmentAsync(string department);
}

public class TeacherManagementService : ITeacherService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<TeacherManagementService> _logger;

    public TeacherManagementService(AuthDbContext context, ILogger<TeacherManagementService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(IEnumerable<TeacherResponseDto> Teachers, int TotalCount)> GetAllTeachersAsync(TeacherQueryDto query)
    {
        var queryable = _context.Teachers.Include(t => t.User).AsQueryable();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(query.SearchQuery))
        {
            var search = query.SearchQuery.ToLower();
            queryable = queryable.Where(t =>
                t.User.FirstName.ToLower().Contains(search) ||
                t.User.LastName.ToLower().Contains(search) ||
                t.User.Email.ToLower().Contains(search) ||
                t.Department.ToLower().Contains(search) ||
                t.Specialization.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            queryable = queryable.Where(t => t.Department.ToLower() == query.Department.ToLower());
        }

        if (query.IsActive.HasValue)
        {
            queryable = queryable.Where(t => t.IsActive == query.IsActive.Value);
        }

        var totalCount = await queryable.CountAsync();

        var teachers = await queryable
            .OrderBy(t => t.User.FirstName)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return (teachers, totalCount);
    }

    public async Task<TeacherResponseDto?> GetTeacherByIdAsync(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TeacherId == id);

        return teacher == null ? null : MapToDto(teacher);
    }

    public async Task<TeacherResponseDto?> GetTeacherByUserIdAsync(int userId)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return teacher == null ? null : MapToDto(teacher);
    }

    public async Task<TeacherResponseDto?> CreateTeacherAsync(CreateTeacherDto dto)
    {
        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
        {
            _logger.LogWarning("Teacher creation failed: Email {Email} already exists", dto.Email);
            return null;
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Create User with Teacher role
            var user = new User
            {
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, 12),
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = UserRole.Teacher,
                IsActive = true,
                AuthProvider = AuthProvider.Local,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Teacher record
            var teacher = new Teacher
            {
                UserId = user.UserId,
                Department = dto.Department,
                Specialization = dto.Specialization,
                Qualification = dto.Qualification,
                Experience = dto.Experience,
                PhoneNumber = dto.PhoneNumber,
                IsActive = true,
                JoiningDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teachers.Add(teacher);
            await _context.SaveChangesAsync();

            // Link teacher to user
            user.TeacherId = teacher.TeacherId;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            _logger.LogInformation("Teacher created successfully: {Email} in {Department}", dto.Email, dto.Department);

            teacher.User = user;
            return MapToDto(teacher);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error creating teacher with email {Email}", dto.Email);
            throw;
        }
    }

    public async Task<TeacherResponseDto?> UpdateTeacherAsync(int id, UpdateTeacherDto dto)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TeacherId == id);

        if (teacher == null)
        {
            _logger.LogWarning("Teacher update failed: Teacher {Id} not found", id);
            return null;
        }

        // Update User fields if provided
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
        {
            teacher.User.FirstName = dto.FirstName;
        }
        if (!string.IsNullOrWhiteSpace(dto.LastName))
        {
            teacher.User.LastName = dto.LastName;
        }
        teacher.User.UpdatedAt = DateTime.UtcNow;

        // Update Teacher fields
        teacher.Department = dto.Department;
        teacher.Specialization = dto.Specialization;
        teacher.Qualification = dto.Qualification;
        teacher.Experience = dto.Experience;
        teacher.PhoneNumber = dto.PhoneNumber;
        teacher.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Teacher updated: {TeacherId}", id);
        return MapToDto(teacher);
    }

    public async Task<bool> DeleteTeacherAsync(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TeacherId == id);

        if (teacher == null)
        {
            _logger.LogWarning("Teacher delete failed: Teacher {Id} not found", id);
            return false;
        }

        // Deactivate instead of hard delete
        teacher.IsActive = false;
        teacher.UpdatedAt = DateTime.UtcNow;
        teacher.User.IsActive = false;
        teacher.User.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Teacher deactivated: {TeacherId}", id);
        return true;
    }

    public async Task<IEnumerable<TeacherResponseDto>> GetTeachersByDepartmentAsync(string department)
    {
        var teachers = await _context.Teachers
            .Include(t => t.User)
            .Where(t => t.Department.ToLower() == department.ToLower() && t.IsActive)
            .OrderBy(t => t.User.FirstName)
            .ToListAsync();

        return teachers.Select(t => MapToDto(t));
    }

    private static TeacherResponseDto MapToDto(Teacher teacher)
    {
        return new TeacherResponseDto
        {
            TeacherId = teacher.TeacherId,
            UserId = teacher.UserId,
            FirstName = teacher.User.FirstName,
            LastName = teacher.User.LastName,
            Email = teacher.User.Email,
            Department = teacher.Department,
            Specialization = teacher.Specialization,
            Qualification = teacher.Qualification,
            Experience = teacher.Experience,
            PhoneNumber = teacher.PhoneNumber,
            PhotoUrl = teacher.User.PhotoUrl ?? teacher.User.ProfilePictureUrl,
            IsActive = teacher.IsActive,
            JoiningDate = teacher.JoiningDate,
            CreatedAt = teacher.CreatedAt
        };
    }
}
