namespace CMS.CourseService.DTOs
{
    public class CreateCourseDto
    {
        public string CourseCode { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Credits { get; set; }
        public int Semester { get; set; }
        public int DepartmentId { get; set; }
    }

    public class UpdateCourseDto
    {
        public string? CourseName { get; set; }
        public string? Description { get; set; }
        public int? Credits { get; set; }
        public int? Semester { get; set; }
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class CreateDepartmentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateDepartmentDto
    {
        public string? Name { get; set; }
        public string? Code { get; set; }
        public bool? IsActive { get; set; }
    }
}
