namespace CMS.AuthService.Models;

public enum UserRole
{
    Student,
    Teacher,
    Admin
}

public enum AuthProvider
{
    Local,
    Google
}

public class User
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; } // Nullable for Google OAuth users
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Student;
    public bool IsActive { get; set; } = true;
    
    // Google OAuth fields
    public string? GoogleId { get; set; }
    public string? ProfilePictureUrl { get; set; } // From Google
    public string? PhotoUrl { get; set; } // Uploaded photo
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    
    // JWT Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public int? StudentId { get; set; }
    public int? TeacherId { get; set; }
}

public class Teacher
{
    public int TeacherId { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Department { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? Qualification { get; set; }
    public int Experience { get; set; } // Years of experience
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
