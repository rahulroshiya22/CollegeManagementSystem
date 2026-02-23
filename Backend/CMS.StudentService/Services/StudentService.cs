using CMS.StudentService.Models;
using CMS.StudentService.DTOs;
using CMS.StudentService.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CMS.StudentService.Services
{
    public interface IStudentService
    {
        Task<(IEnumerable<Student> Students, int TotalCount)> GetAllStudentsAsync(StudentQueryDto query);
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student> CreateStudentAsync(CreateStudentDto dto);
        Task UpdateStudentAsync(int id, UpdateStudentDto dto);
        Task UpdateStudentStatusAsync(int id, string status);
        Task DeleteStudentAsync(int id);
        Task<bool> StudentExistsAsync(int id);
    }

    public class StudentService : IStudentService
    {
        private readonly IStudentRepository _repository;
        private readonly IDistributedCache _cache;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            IStudentRepository repository, 
            IDistributedCache cache,
            ILogger<StudentService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(IEnumerable<Student> Students, int TotalCount)> GetAllStudentsAsync(StudentQueryDto query)
        {
            // Direct database query - cache disabled to avoid stale data issues
            _logger.LogInformation("Fetching students from database (cache bypassed)");
            return await _repository.GetAllAsync(query);
        }

        public async Task<Student?> GetStudentByIdAsync(int id)
        {
            string cacheKey = $"student_{id}";

            try
            {
                // Try to get from cache
                string? cachedData = await _cache.GetStringAsync(cacheKey);

                if (!string.IsNullOrEmpty(cachedData))
                {
                    _logger.LogInformation($"✅ Cache HIT: Student {id} from Redis");
                    return JsonSerializer.Deserialize<Student>(cachedData);
                }

                // Cache MISS - Get from database
                _logger.LogInformation($"❌ Cache MISS: Fetching student {id} from database");
                var student = await _repository.GetByIdAsync(id);

                if (student != null)
                {
                    // Store in cache for 10 minutes
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                    };

                    string serializedData = JsonSerializer.Serialize(student);
                    await _cache.SetStringAsync(cacheKey, serializedData, cacheOptions);
                }

                return student;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"⚠️ Redis cache failed for student {id}, falling back to database");
                return await _repository.GetByIdAsync(id);
            }
        }

        public async Task<Student> CreateStudentAsync(CreateStudentDto dto)
        {
            // Check if email already exists
            var existingEmail = await _repository.GetByEmailAsync(dto.Email);
            if (existingEmail != null)
                throw new InvalidOperationException($"Student with email {dto.Email} already exists");

            // Check if roll number already exists
            var existingRoll = await _repository.GetByRollNumberAsync(dto.RollNumber);
            if (existingRoll != null)
                throw new InvalidOperationException($"Student with roll number {dto.RollNumber} already exists");

            var student = new Student
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Phone = dto.Phone,
                RollNumber = dto.RollNumber,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                Address = dto.Address,
                DepartmentId = dto.DepartmentId,
                AdmissionYear = dto.AdmissionYear,
                Status = "Active"
            };

            await _repository.AddAsync(student);
            _logger.LogInformation("Created student {RollNumber}", student.RollNumber);
            
            // Invalidate students list cache
            try
            {
                await _cache.RemoveAsync("students_all");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after creating student");
            }
            
            return student;
        }

        public async Task UpdateStudentAsync(int id, UpdateStudentDto dto)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null)
                throw new KeyNotFoundException($"Student with ID {id} not found");

            // Check if email is being changed and if it's already taken
            if (student.Email != dto.Email)
            {
                var existingEmail = await _repository.GetByEmailAsync(dto.Email);
                if (existingEmail != null)
                    throw new InvalidOperationException($"Email {dto.Email} is already in use");
            }

            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.Email = dto.Email;
            student.Phone = dto.Phone;
            student.Address = dto.Address;
            student.DepartmentId = dto.DepartmentId;

            await _repository.UpdateAsync(student);
            _logger.LogInformation("Updated student {RollNumber}", student.RollNumber);
            
            // Invalidate cache
            try
            {
                await _cache.RemoveAsync($"student_{id}");
                await _cache.RemoveAsync("students_all");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after updating student");
            }
        }

        public async Task UpdateStudentStatusAsync(int id, string status)
        {
            var student = await _repository.GetByIdAsync(id);
            if (student == null)
                throw new KeyNotFoundException($"Student with ID {id} not found");

            var validStatuses = new[] { "Active", "Inactive", "Graduated", "Suspended" };
            if (!validStatuses.Contains(status))
                throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");

            student.Status = status;
            await _repository.UpdateAsync(student);
            _logger.LogInformation("Updated student {RollNumber} status to {Status}", student.RollNumber, status);
            
            // Invalidate cache
            try
            {
                await _cache.RemoveAsync($"student_{id}");
                await _cache.RemoveAsync("students_all");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after updating student status");
            }
        }

        public async Task DeleteStudentAsync(int id)
        {
            if (!await _repository.ExistsAsync(id))
                throw new KeyNotFoundException($"Student with ID {id} not found");

            await _repository.DeleteAsync(id);
            _logger.LogInformation("Deleted student with ID {StudentId}", id);
            
            // Invalidate cache
            try
            {
                await _cache.RemoveAsync($"student_{id}");
                await _cache.RemoveAsync("students_all");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache after deleting student");
            }
        }

        public async Task<bool> StudentExistsAsync(int id)
        {
            return await _repository.ExistsAsync(id);
        }
    }
}
