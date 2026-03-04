namespace CMS.AcademicService.Models
{
    public class TimeSlot
    {
        public int TimeSlotId { get; set; }
        public int CourseId { get; set; }
        public int? TeacherId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty; // Monday, Tuesday, etc.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Grade
    {
        public int GradeId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal Marks { get; set; }
        public string GradeLetter { get; set; } = string.Empty; // A+, A, B+, B, C, D, F
        public int Semester { get; set; }
        public int Year { get; set; }
        public string? Remarks { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class Notice
    {
        public int NoticeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "General"; // General, Academic, Exam, Event
        public string? TargetRole { get; set; } // null = All, "Student", "Teacher", "Admin"
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
