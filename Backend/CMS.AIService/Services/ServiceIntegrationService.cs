using CMS.AIService.Models;
using System.Text.Json;

namespace CMS.AIService.Services;

public class ServiceIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceIntegrationService> _logger;
    private readonly string _apiGatewayUrl;

    public ServiceIntegrationService(HttpClient httpClient, IConfiguration config, ILogger<ServiceIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiGatewayUrl = config["Services:ApiGatewayUrl"] ?? "https://localhost:7000";
    }

    public async Task<StudentData> GetStudentDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/student");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var students = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (students != null)
                {
                    return new StudentData
                    {
                        TotalStudents = students.Count,
                        StudentNames = students.Take(5).Select(s => 
                        {
                            var firstName = s.TryGetProperty("firstName", out var fn) ? fn.GetString() : "";
                            var lastName = s.TryGetProperty("lastName", out var ln) ? ln.GetString() : "";
                            return $"{firstName} {lastName}";
                        }).ToList()
                    };
                }
            }

            return new StudentData { TotalStudents = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student data");
            return new StudentData { TotalStudents = 0 };
        }
    }

    public async Task<CourseData> GetCourseDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/course");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var courses = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (courses != null)
                {
                    return new CourseData
                    {
                        TotalCourses = courses.Count,
                        CourseNames = courses.Take(5).Select(c => 
                        {
                            var name = c.TryGetProperty("name", out var n) ? n.GetString() : "Unknown";
                            return name ?? "Unknown";
                        }).ToList()
                    };
                }
            }

            return new CourseData { TotalCourses = 0 };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching course data");
            return new CourseData { TotalCourses = 0 };
        }
    }

    public async Task<FeeData> GetFeeDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/fee");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var fees = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (fees != null)
                {
                    var pending = fees.Where(f => 
                    {
                        if (f.TryGetProperty("status", out var status))
                        {
                            var statusStr = status.GetString()?.ToLower();
                            return statusStr == "pending" || statusStr == "unpaid";
                        }
                        return false;
                    }).ToList();

                    var totalPending = pending.Sum(f =>
                    {
                        if (f.TryGetProperty("amount", out var amt))
                            return amt.GetDecimal();
                        return 0;
                    });

                    return new FeeData
                    {
                        TotalPending = totalPending,
                        StudentsWithPending = pending.Count
                    };
                }
            }

            return new FeeData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching fee data");
            return new FeeData();
        }
    }

    public async Task<AttendanceData> GetAttendanceDataAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/attendance");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var records = JsonSerializer.Deserialize<List<JsonElement>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (records != null && records.Count > 0)
                {
                    var presentCount = records.Count(r =>
                    {
                        if (r.TryGetProperty("isPresent", out var present))
                            return present.GetBoolean();
                        return false;
                    });

                    return new AttendanceData
                    {
                        AverageAttendance = (double)presentCount / records.Count * 100,
                        TotalRecords = records.Count
                    };
                }
            }

            return new AttendanceData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching attendance data");
            return new AttendanceData();
        }
    }

    // ======================= WRITE OPERATIONS =======================
    
    // Student Write Operations
    public async Task<int> CreateStudentAsync(CreateStudentDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiGatewayUrl}/api/student", dto);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var student = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return student.TryGetProperty("studentId", out var id) ? id.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            throw;
        }
    }

    public async Task<object?> GetStudentByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/student/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by ID");
            return null;
        }
    }

    public async Task UpdateStudentAsync(int id, UpdateStudentDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiGatewayUrl}/api/student/{id}", dto);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student");
            throw;
        }
    }

    public async Task DeleteStudentAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiGatewayUrl}/api/student/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student");
            throw;
        }
    }

    // Course Write Operations
    public async Task<int> CreateCourseAsync(CreateCourseDto dto)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{_apiGatewayUrl}/api/course", dto);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var course = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return course.TryGetProperty("courseId", out var id) ? id.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating course");
            throw;
        }
    }

    public async Task<object?> GetCourseByIdAsync(int id)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/course/{id}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course by ID");
            return null;
        }
    }

    public async Task UpdateCourseAsync(int id, UpdateCourseDto dto)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"{_apiGatewayUrl}/api/course/{id}", dto);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating course");
            throw;
        }
    }

    public async Task DeleteCourseAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiGatewayUrl}/api/course/{id}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting course");
            throw;
        }
    }

    // Enrollment Operations
    public async Task<object> GetEnrollmentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/api/enrollment");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<object>(content) ?? new object();
            }
            return new object();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrollments");
            return new object();
        }
    }

    public async Task<int> EnrollStudentAsync(CreateEnrollmentDto dto)
    {
        try
        {
            var enrollment = new
            {
                studentId = dto.StudentId,
                courseId = dto.CourseId,
                semester = dto.Semester,
                status = dto.Status,
                enrollmentDate = DateTime.UtcNow
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_apiGatewayUrl}/api/enrollment", enrollment);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result.TryGetProperty("enrollmentId", out var id) ? id.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enrolling student");
            throw;
        }
    }

    public async Task DropEnrollmentAsync(int enrollmentId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{_apiGatewayUrl}/api/enrollment/{enrollmentId}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dropping enrollment");
            throw;
        }
    }

    // Fee Operations
    public async Task RecordPaymentAsync(int feeId)
    {
        try
        {
            var response = await _httpClient.PostAsync($"{_apiGatewayUrl}/api/fee/{feeId}/pay", null);
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment");
            throw;
        }
    }

    public async Task<int> CreateFeeAsync(CreateFeeDto dto)
    {
        try
        {
            var fee = new
            {
                studentId = dto.StudentId,
                amount = dto.Amount,
                feeType = dto.FeeType,
                dueDate = dto.DueDate,
                status = dto.Status,
                createdDate = DateTime.UtcNow
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_apiGatewayUrl}/api/fee", fee);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result.TryGetProperty("feeId", out var id) ? id.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fee");
            throw;
        }
    }

    // Attendance Operations
    public async Task<int> MarkAttendanceAsync(MarkAttendanceDto dto)
    {
        try
        {
            var attendance = new
            {
                studentId = dto.StudentId,
                courseId = dto.CourseId,
                date = dto.Date,
                isPresent = dto.IsPresent,
                markedAt = DateTime.UtcNow
            };
            
            var response = await _httpClient.PostAsJsonAsync($"{_apiGatewayUrl}/api/attendance", attendance);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result.TryGetProperty("attendanceId", out var id) ? id.GetInt32() : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking attendance");
            throw;
        }
    }
}
