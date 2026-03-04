namespace CMS.AuthService.DTOs;

public class CreateTeacherDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public int Experience { get; set; }
    public string? PhoneNumber { get; set; }
}

public class UpdateTeacherDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public int Experience { get; set; }
    public string? PhoneNumber { get; set; }
}

public class TeacherResponseDto
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public int Experience { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoiningDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TeacherQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchQuery { get; set; }
    public string? Department { get; set; }
    public bool? IsActive { get; set; }
}
