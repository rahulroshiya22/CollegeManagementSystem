using CMS.AcademicService.Models;
using Microsoft.EntityFrameworkCore;

namespace CMS.AcademicService.Data
{
    public class AcademicDbContext : DbContext
    {
        public AcademicDbContext(DbContextOptions<AcademicDbContext> options) : base(options) { }

        public DbSet<TimeSlot> TimeSlots { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Notice> Notices { get; set; }
        
        // Messaging System
        public DbSet<Message> Messages { get; set; }
        public DbSet<GroupAnnouncement> GroupAnnouncements { get; set; }
        
        // Examination System
        public DbSet<Exam> Exams { get; set; }
        public DbSet<ExamQuestion> ExamQuestions { get; set; }
        public DbSet<ExamSubmission> ExamSubmissions { get; set; }
        public DbSet<ExamAnswer> ExamAnswers { get; set; }
        public DbSet<ExamResult> ExamResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TimeSlot>(entity =>
            {
                entity.HasKey(e => e.TimeSlotId);
            });

            modelBuilder.Entity<Grade>(entity =>
            {
                entity.HasKey(e => e.GradeId);
                entity.Property(e => e.Marks).HasColumnType("decimal(5,2)");
                entity.HasIndex(e => new { e.StudentId, e.CourseId, e.Semester, e.Year }).IsUnique();
            });

            modelBuilder.Entity<Notice>(entity =>
            {
                entity.HasKey(e => e.NoticeId);
            });

            // Messaging System
            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageId);
            });

            modelBuilder.Entity<GroupAnnouncement>(entity =>
            {
                entity.HasKey(e => e.AnnouncementId);
            });

            // Examination System
            modelBuilder.Entity<Exam>(entity =>
            {
                entity.HasKey(e => e.ExamId);
            });

            modelBuilder.Entity<ExamQuestion>(entity =>
            {
                entity.HasKey(e => e.QuestionId);
            });

            modelBuilder.Entity<ExamSubmission>(entity =>
            {
                entity.HasKey(e => e.SubmissionId);
            });

            modelBuilder.Entity<ExamAnswer>(entity =>
            {
                entity.HasKey(e => e.AnswerId);
            });

            modelBuilder.Entity<ExamResult>(entity =>
            {
                entity.HasKey(e => e.ResultId);
            });

            var dt = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);

            // Seed Timetable (30 slots)
            modelBuilder.Entity<TimeSlot>().HasData(
                new TimeSlot { TimeSlotId=1,CourseId=1,TeacherId=1,DayOfWeek="Monday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="CS-301",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=2,CourseId=1,TeacherId=1,DayOfWeek="Wednesday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="CS-301",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=3,CourseId=2,TeacherId=1,DayOfWeek="Monday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="CS-302",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=4,CourseId=2,TeacherId=1,DayOfWeek="Thursday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="CS-302",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=5,CourseId=3,TeacherId=2,DayOfWeek="Tuesday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="CS-Lab1",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=6,CourseId=3,TeacherId=2,DayOfWeek="Friday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="CS-Lab1",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=7,CourseId=4,TeacherId=1,DayOfWeek="Tuesday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="CS-303",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=8,CourseId=5,TeacherId=2,DayOfWeek="Wednesday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="CS-304",Semester=5,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=9,CourseId=6,TeacherId=2,DayOfWeek="Thursday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,30,0),Room="AI-Lab",Semester=5,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=10,CourseId=7,TeacherId=3,DayOfWeek="Monday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="IT-201",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=11,CourseId=7,TeacherId=3,DayOfWeek="Wednesday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="IT-201",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=12,CourseId=8,TeacherId=3,DayOfWeek="Tuesday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="IT-202",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=13,CourseId=9,TeacherId=4,DayOfWeek="Monday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="IT-Lab1",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=14,CourseId=9,TeacherId=4,DayOfWeek="Thursday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="IT-Lab1",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=15,CourseId=10,TeacherId=4,DayOfWeek="Friday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="IT-203",Semester=3,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=16,CourseId=11,TeacherId=5,DayOfWeek="Monday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,0,0),Room="ECE-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=17,CourseId=11,TeacherId=5,DayOfWeek="Wednesday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,0,0),Room="ECE-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=18,CourseId=12,TeacherId=5,DayOfWeek="Tuesday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,0,0),Room="ECE-Lab",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=19,CourseId=14,TeacherId=6,DayOfWeek="Monday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="ME-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=20,CourseId=14,TeacherId=6,DayOfWeek="Wednesday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="ME-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=21,CourseId=15,TeacherId=6,DayOfWeek="Tuesday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="ME-Lab",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=22,CourseId=16,TeacherId=7,DayOfWeek="Thursday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,0,0),Room="CE-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=23,CourseId=17,TeacherId=7,DayOfWeek="Friday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,30,0),Room="CE-Lab",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=24,CourseId=18,TeacherId=8,DayOfWeek="Monday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="EE-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=25,CourseId=18,TeacherId=8,DayOfWeek="Wednesday",StartTime=new TimeSpan(10,15,0),EndTime=new TimeSpan(11,15,0),Room="EE-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=26,CourseId=19,TeacherId=9,DayOfWeek="Tuesday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="MATH-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=27,CourseId=19,TeacherId=9,DayOfWeek="Thursday",StartTime=new TimeSpan(11,30,0),EndTime=new TimeSpan(12,30,0),Room="MATH-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=28,CourseId=20,TeacherId=10,DayOfWeek="Friday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="PHY-101",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=29,CourseId=20,TeacherId=10,DayOfWeek="Wednesday",StartTime=new TimeSpan(14,0,0),EndTime=new TimeSpan(15,0,0),Room="PHY-Lab",Semester=1,Year=2024,IsActive=true,CreatedAt=dt },
                new TimeSlot { TimeSlotId=30,CourseId=13,TeacherId=5,DayOfWeek="Thursday",StartTime=new TimeSpan(9,0,0),EndTime=new TimeSpan(10,0,0),Room="ECE-201",Semester=3,Year=2024,IsActive=true,CreatedAt=dt }
            );

            // Seed Notices (10)
            var ndt = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc);
            modelBuilder.Entity<Notice>().HasData(
                new Notice { NoticeId=1, Title="Welcome to Semester 2 — 2025", Content="Dear students, welcome back! Classes begin on Jan 20.", Category="Academic", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=2, Title="Mid-Semester Exam Schedule", Content="Mid-semester exams: March 10-20. Check timetable.", Category="Exam", TargetRole="Student", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=3, Title="Annual Sports Day", Content="Annual sports day on Feb 28. All are welcome!", Category="Event", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=4, Title="Library Extended Hours", Content="Library open until 10 PM during exam season.", Category="General", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=5, Title="Faculty Meeting", Content="Monthly faculty meeting on March 1 at 3 PM.", Category="General", TargetRole="Teacher", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=6, Title="Scholarship Applications Open", Content="Merit scholarship applications for 2025 now open. Deadline: March 15.", Category="Academic", TargetRole="Student", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=7, Title="Hackathon 2025", Content="24-hour hackathon on March 25. Register at CS dept.", Category="Event", TargetRole="Student", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=8, Title="New Lab Equipment", Content="New IoT lab equipment arrived. Training starts March 5.", Category="Academic", TargetRole="Teacher", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=9, Title="Holiday Notice — Holi", Content="College closed on March 14 for Holi.", Category="General", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt },
                new Notice { NoticeId=10, Title="Placement Drive — TCS", Content="TCS campus placement on April 5. Eligibility: 7+ CGPA.", Category="Academic", TargetRole="Student", CreatedByUserId=1, CreatedByName="Admin", IsActive=true, CreatedAt=ndt }
            );
        }
    }
}
