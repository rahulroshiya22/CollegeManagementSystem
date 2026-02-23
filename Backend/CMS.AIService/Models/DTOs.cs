namespace CMS.AIService.Models;

// Student DTOs
public class CreateStudentDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RollNumber { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public int AdmissionYear { get; set; }
}

public class UpdateStudentDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public int? DepartmentId { get; set; }
}

// Course DTOs
public class CreateCourseDto
{
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Semester { get; set; }
    public int DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateCourseDto
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Credits { get; set; }
    public int Semester { get; set; }
    public int DepartmentId { get; set; }
    public bool IsActive { get; set; } = true;
}

// Enrollment DTOs
public class CreateEnrollmentDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public string Semester { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
}

// Fee DTOs
public class RecordPaymentDto
{
    public int FeeId { get; set; }
}

public class CreateFeeDto
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string FeeType { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = "Pending";
}

// Attendance DTOs
public class MarkAttendanceDto
{
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime Date { get; set; }
    public bool IsPresent { get; set; }
}
