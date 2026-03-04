namespace CMS.AcademicService.Models
{
    public class Message
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string SenderRole { get; set; } = string.Empty; // Student, Teacher, Admin
        public string ReceiverRole { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public int? ParentMessageId { get; set; } // For threading
        public string? AttachmentUrl { get; set; }
    }

    public class GroupAnnouncement
    {
        public int AnnouncementId { get; set; }
        public int CreatorId { get; set; }
        public string CreatorRole { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty; // All, Department, Semester, Course
        public string? TargetFilter { get; set; } // e.g., "CS", "Semester-3"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}
