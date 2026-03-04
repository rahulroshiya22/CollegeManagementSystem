using System.Text.Json;

namespace CMS.AIAssistantService.Services;

public class ServiceIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ServiceIntegrationService> _logger;
    private readonly string _apiGatewayUrl;

    public ServiceIntegrationService(HttpClient httpClient, IConfiguration configuration, ILogger<ServiceIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiGatewayUrl = configuration["ApiGateway:BaseUrl"] ?? "https://localhost:7000";
    }

    public async Task<string?> GetStudentInfoAsync(int studentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/students/{studentId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Retrieved student info for ID {StudentId}", studentId);
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student info for ID {StudentId}", studentId);
        }
        return null;
    }

    public async Task<string?> GetAttendanceInfoAsync(int studentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/attendance/student/{studentId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Retrieved attendance info for student {StudentId}", studentId);
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting attendance info for student {StudentId}", studentId);
        }
        return null;
    }

    public async Task<string?> GetFeeInfoAsync(int studentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/fees/student/{studentId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Retrieved fee info for student {StudentId}", studentId);
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fee info for student {StudentId}", studentId);
        }
        return null;
    }

    public async Task<string?> GetCoursesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/courses");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Retrieved courses list");
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting courses");
        }
        return null;
    }

    public async Task<string?> GetEnrollmentsAsync(int studentId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiGatewayUrl}/enrollments/student/{studentId}");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Retrieved enrollments for student {StudentId}", studentId);
                return content;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrollments for student {StudentId}", studentId);
        }
        return null;
    }
}
