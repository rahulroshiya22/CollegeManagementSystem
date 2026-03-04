using CMS.AcademicService.Data;
using CMS.AcademicService.DTOs;
using CMS.AcademicService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMS.AcademicService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExamController : ControllerBase
    {
        private readonly AcademicDbContext _context;

        public ExamController(AcademicDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? courseId)
        {
            var query = _context.Exams.AsQueryable();

            if (courseId.HasValue)
                query = query.Where(e => e.CourseId == courseId.Value);

            var exams = await query.OrderByDescending(e => e.ScheduledDate).ToListAsync();
            return Ok(exams);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();
            return Ok(exam);
        }

        [HttpGet("{id}/questions")]
        public async Task<IActionResult> GetQuestions(int id)
        {
            var questions = await _context.ExamQuestions
                .Where(q => q.ExamId == id)
                .OrderBy(q => q.OrderIndex)
                .ToListAsync();

            return Ok(questions);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateExamDto dto, [FromQuery] int teacherId)
        {
            var exam = new Exam
            {
                Title = dto.Title,
                Description = dto.Description,
                CourseId = dto.CourseId,
                CreatedByTeacherId = teacherId,
                ScheduledDate = dto.ScheduledDate,
                Duration = TimeSpan.FromMinutes(dto.DurationMinutes),
                TotalMarks = dto.TotalMarks,
                PassingMarks = dto.PassingMarks,
                ExamType = dto.ExamType,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = exam.ExamId }, exam);
        }

        [HttpPost("{id}/questions")]
        public async Task<IActionResult> AddQuestion(int id, [FromBody] CreateQuestionDto dto)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            var maxOrder = await _context.ExamQuestions
                .Where(q => q.ExamId == id)
                .MaxAsync(q => (int?)q.OrderIndex) ?? 0;

            var question = new ExamQuestion
            {
                ExamId = id,
                QuestionText = dto.QuestionText,
                QuestionType = dto.QuestionType,
                Marks = dto.Marks,
                OrderIndex = maxOrder + 1,
                OptionA = dto.OptionA,
                OptionB = dto.OptionB,
                OptionC = dto.OptionC,
                OptionD = dto.OptionD,
                CorrectAnswer = dto.CorrectAnswer
            };

            _context.ExamQuestions.Add(question);
            await _context.SaveChangesAsync();

            return Ok(question);
        }

        [HttpPut("{id}/publish")]
        public async Task<IActionResult> Publish(int id)
        {
            var exam = await _context.Exams.FindAsync(id);
            if (exam == null) return NotFound();

            exam.IsPublished = true;
            exam.PublishedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(exam);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitExam([FromBody] SubmitExamDto dto, [FromQuery] int studentId)
        {
            var exam = await _context.Exams.FindAsync(dto.ExamId);
            if (exam == null) return NotFound("Exam not found");

            var questions = await _context.ExamQuestions
                .Where(q => q.ExamId == dto.ExamId)
                .ToListAsync();

            var submission = new ExamSubmission
            {
                ExamId = dto.ExamId,
                StudentId = studentId,
                StartedAt = DateTime.UtcNow.AddMinutes(-exam.Duration.TotalMinutes),
                SubmittedAt = DateTime.UtcNow,
                IsCompleted = true,
                Status = "Submitted"
            };

            _context.ExamSubmissions.Add(submission);
            await _context.SaveChangesAsync();

            int totalMarks = 0;
            foreach (var answer in dto.Answers)
            {
                var question = questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (question == null) continue;

                bool isCorrect = question.CorrectAnswer?.Trim().Equals(answer.StudentAnswer?.Trim(), StringComparison.OrdinalIgnoreCase) ?? false;
                int marksAwarded = isCorrect ? question.Marks : 0;
                totalMarks += marksAwarded;

                var examAnswer = new ExamAnswer
                {
                    SubmissionId = submission.SubmissionId,
                    QuestionId = answer.QuestionId,
                    StudentAnswer = answer.StudentAnswer,
                    MarksAwarded = marksAwarded,
                    IsCorrect = isCorrect
                };

                _context.ExamAnswers.Add(examAnswer);
            }

            submission.ObtainedMarks = totalMarks;
            submission.Status = "Evaluated";

            var percentage = (decimal)totalMarks / exam.TotalMarks * 100;
            var grade = CalculateGrade(percentage);

            var result = new ExamResult
            {
                ExamId = dto.ExamId,
                StudentId = studentId,
                ObtainedMarks = totalMarks,
                TotalMarks = exam.TotalMarks,
                Percentage = percentage,
                Grade = grade,
                IsPassed = totalMarks >= exam.PassingMarks,
                EvaluatedAt = DateTime.UtcNow,
                EvaluatedByTeacherId = exam.CreatedByTeacherId
            };

            _context.ExamResults.Add(result);
            await _context.SaveChangesAsync();

            return Ok(new { submission, result });
        }

        [HttpGet("results/student/{studentId}")]
        public async Task<IActionResult> GetStudentResults(int studentId)
        {
            var results = await _context.ExamResults
                .Where(r => r.StudentId == studentId)
                .ToListAsync();

            return Ok(results);
        }

        [HttpGet("results/exam/{examId}")]
        public async Task<IActionResult> GetExamResults(int examId)
        {
            var results = await _context.ExamResults
                .Where(r => r.ExamId == examId)
                .OrderByDescending(r => r.ObtainedMarks)
                .ToListAsync();

            return Ok(results);
        }

        private string CalculateGrade(decimal percentage)
        {
            if (percentage >= 90) return "A+";
            if (percentage >= 80) return "A";
            if (percentage >= 70) return "B+";
            if (percentage >= 60) return "B";
            if (percentage >= 50) return "C";
            if (percentage >= 40) return "D";
            return "F";
        }
    }
}
