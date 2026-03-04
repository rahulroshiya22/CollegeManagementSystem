namespace CMS.StudentService.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string RollNumber { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; } = string.Empty; // Male, Female, Other
        public string Address { get; set; } = string.Empty;
        
        // Academic Info
        public int DepartmentId { get; set; }
        public int AdmissionYear { get; set; }
        public string Status { get; set; } = "Active"; // Active, Inactive, Graduated, Suspended
        
        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public Department Department { get; set; } = null!;
    }
}
