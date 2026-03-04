namespace CMS.AcademicService.Models
{
    public class Exam
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public int CreatedByTeacherId { get; set; }
        public DateTime ScheduledDate { get; set; }
        public TimeSpan Duration { get; set; } // e.g., 2 hours
        public int TotalMarks { get; set; }
        public int PassingMarks { get; set; }
        public string ExamType { get; set; } = string.Empty; // MCQ, Descriptive, Mixed
        public bool IsPublished { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
    }

    public class ExamQuestion
    {
        public int QuestionId { get; set; }
        public int ExamId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public string QuestionType { get; set; } = string.Empty; // MCQ, TrueFalse, ShortAnswer, Essay
        public int Marks { get; set; }
        public int OrderIndex { get; set; }
        
        // For MCQ
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string? CorrectAnswer { get; set; } // A, B, C, D or text for other types
    }

    public class ExamSubmission
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public bool IsCompleted { get; set; } = false;
        public int? ObtainedMarks { get; set; }
        public string Status { get; set; } = "InProgress"; // InProgress, Submitted, Evaluated
    }

    public class ExamAnswer
    {
        public int AnswerId { get; set; }
        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public string StudentAnswer { get; set; } = string.Empty;
        public int? MarksAwarded { get; set; }
        public bool IsCorrect { get; set; } = false;
    }

    public class ExamResult
    {
        public int ResultId { get; set; }
        public int ExamId { get; set; }
        public int StudentId { get; set; }
        public int ObtainedMarks { get; set; }
        public int TotalMarks { get; set; }
        public decimal Percentage { get; set; }
        public string Grade { get; set; } = string.Empty; // A+, A, B+, etc.
        public bool IsPassed { get; set; }
        public DateTime EvaluatedAt { get; set; }
        public int EvaluatedByTeacherId { get; set; }
    }
}
