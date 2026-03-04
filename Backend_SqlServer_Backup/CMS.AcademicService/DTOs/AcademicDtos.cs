namespace CMS.AcademicService.DTOs
{
    // TimeSlot DTOs
    public class CreateTimeSlotDto
    {
        public int CourseId { get; set; }
        public int? TeacherId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string? Room { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }
    }

    public class UpdateTimeSlotDto
    {
        public int? CourseId { get; set; }
        public int? TeacherId { get; set; }
        public string? DayOfWeek { get; set; }
        public TimeSpan? StartTime { get; set; }
        public TimeSpan? EndTime { get; set; }
        public string? Room { get; set; }
        public bool? IsActive { get; set; }
    }

    // Grade DTOs
    public class CreateGradeDto
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public decimal Marks { get; set; }
        public string GradeLetter { get; set; } = string.Empty;
        public int Semester { get; set; }
        public int Year { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateGradeDto
    {
        public decimal? Marks { get; set; }
        public string? GradeLetter { get; set; }
        public string? Remarks { get; set; }
    }

    // Notice DTOs
    public class CreateNoticeDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string? TargetRole { get; set; }
        public int CreatedByUserId { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
    }

    public class UpdateNoticeDto
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Category { get; set; }
        public string? TargetRole { get; set; }
        public bool? IsActive { get; set; }
    }
}
