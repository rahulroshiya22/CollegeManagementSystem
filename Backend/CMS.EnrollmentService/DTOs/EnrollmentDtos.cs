namespace CMS.EnrollmentService.DTOs
{
    public class CreateEnrollmentDto
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public int Semester { get; set; }
        public int Year { get; set; }
    }

    public class UpdateEnrollmentDto
    {
        public string? Status { get; set; }
        public decimal? Grade { get; set; }
    }
}
