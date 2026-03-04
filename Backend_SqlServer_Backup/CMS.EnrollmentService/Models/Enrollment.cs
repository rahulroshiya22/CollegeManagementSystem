namespace CMS.EnrollmentService.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrollmentDate { get; set; } = DateTime.UtcNow;
        public int Semester { get; set; }
        public int Year { get; set; }
        public string Status { get; set; } = "Active"; // Active, Completed, Dropped
        public decimal? Grade { get; set; }
    }
}
