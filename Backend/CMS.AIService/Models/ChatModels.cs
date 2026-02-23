namespace CMS.AIService.Models;

public class ChatRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Response { get; set; } = string.Empty;
}

public class ChatMessage
{
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

// Models for service integration
public class StudentData
{
    public int TotalStudents { get; set; }
    public List<string> StudentNames { get; set; } = new();
}

public class CourseData
{
    public int TotalCourses { get; set; }
    public List<string> CourseNames { get; set; } = new();
}

public class FeeData
{
    public decimal TotalPending { get; set; }
    public int StudentsWithPending { get; set; }
}

public class AttendanceData
{
    public double AverageAttendance { get; set; }
    public int TotalRecords { get; set; }
}
