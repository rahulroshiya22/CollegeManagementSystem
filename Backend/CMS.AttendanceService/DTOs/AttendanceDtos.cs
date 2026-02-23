namespace CMS.AttendanceService.DTOs
{
    public class CreateAttendanceDto
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime Date { get; set; }
        public bool IsPresent { get; set; }
        public string? Remarks { get; set; }
    }

    public class UpdateAttendanceDto
    {
        public bool IsPresent { get; set; }
        public string? Remarks { get; set; }
    }

    public class BulkAttendanceDto
    {
        public int CourseId { get; set; }
        public DateTime Date { get; set; }
        public List<StudentAttendanceDto> Students { get; set; } = new();
    }

    public class StudentAttendanceDto
    {
        public int StudentId { get; set; }
        public bool IsPresent { get; set; }
        public string? Remarks { get; set; }
    }
}
