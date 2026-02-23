namespace CMS.AcademicService.DTOs
{
    // Message DTOs
    public class CreateMessageDto
    {
        public int ReceiverId { get; set; }
        public string ReceiverRole { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int? ParentMessageId { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    public class MessageDto
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string SenderRole { get; set; } = string.Empty;
        public string ReceiverRole { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public int? ParentMessageId { get; set; }
        public string? AttachmentUrl { get; set; }
    }

    // Announcement DTOs
    public class CreateAnnouncementDto
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = "All";
        public string? TargetFilter { get; set; }
    }

    public class AnnouncementDto
    {
        public int AnnouncementId { get; set; }
        public int CreatorId { get; set; }
        public string CreatorRole { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string TargetAudience { get; set; } = string.Empty;
        public string? TargetFilter { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // Exam DTOs
    public class CreateExamDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public int DurationMinutes { get; set; }
        public int TotalMarks { get; set; }
        public int PassingMarks { get; set; }
        public string ExamType { get; set; } = "MCQ";
    }

    public class CreateQuestionDto
    {
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MCQ";
        public int Marks { get; set; }
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; }
    }

    public class SubmitExamDto
    {
        public int ExamId { get; set; }
        public List<AnswerDto> Answers { get; set; } = new();
    }

    public class AnswerDto
    {
        public int QuestionId { get; set; }
        public string StudentAnswer { get; set; } = string.Empty;
    }

    public class ExamResultDto
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public int ObtainedMarks { get; set; }
        public int TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; } = string.Empty;
        public bool IsPassed { get; set; }
        public DateTime EvaluatedAt { get; set; }
    }
}
