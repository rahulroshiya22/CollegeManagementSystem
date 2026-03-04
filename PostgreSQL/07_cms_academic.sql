-- ============================================================
-- CMS ACADEMIC DATABASE - PostgreSQL
-- Original SQL Server DB: CMS_AcademicDb
-- ============================================================
-- Run: psql -U postgres -d cms_academic -f 07_cms_academic.sql
-- ============================================================

-- ==================== TIMETABLE ====================
CREATE TABLE IF NOT EXISTS "TimeSlots" (
    "TimeSlotId"   SERIAL PRIMARY KEY,
    "CourseId"     INTEGER NOT NULL,
    "TeacherId"    INTEGER,
    "DayOfWeek"    TEXT NOT NULL DEFAULT '',
    "StartTime"    TIME NOT NULL,
    "EndTime"      TIME NOT NULL,
    "Room"         TEXT,
    "Semester"     INTEGER NOT NULL DEFAULT 0,
    "Year"         INTEGER NOT NULL DEFAULT 0,
    "IsActive"     BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"    TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ==================== GRADES ====================
CREATE TABLE IF NOT EXISTS "Grades" (
    "GradeId"      SERIAL PRIMARY KEY,
    "StudentId"    INTEGER NOT NULL,
    "CourseId"     INTEGER NOT NULL,
    "Marks"        NUMERIC(5,2) NOT NULL DEFAULT 0,
    "GradeLetter"  TEXT NOT NULL DEFAULT '',
    "Semester"     INTEGER NOT NULL DEFAULT 0,
    "Year"         INTEGER NOT NULL DEFAULT 0,
    "Remarks"      TEXT,
    "CreatedAt"    TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"    TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Grades_Student_Course_Sem_Year"
    ON "Grades" ("StudentId", "CourseId", "Semester", "Year");

-- ==================== NOTICES ====================
CREATE TABLE IF NOT EXISTS "Notices" (
    "NoticeId"        SERIAL PRIMARY KEY,
    "Title"           TEXT NOT NULL DEFAULT '',
    "Content"         TEXT NOT NULL DEFAULT '',
    "Category"        TEXT NOT NULL DEFAULT 'General',
    "TargetRole"      TEXT,
    "CreatedByUserId" INTEGER NOT NULL DEFAULT 0,
    "CreatedByName"   TEXT NOT NULL DEFAULT '',
    "IsActive"        BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"       TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"       TIMESTAMP
);

-- ==================== MESSAGING ====================
CREATE TABLE IF NOT EXISTS "Messages" (
    "MessageId"        SERIAL PRIMARY KEY,
    "SenderId"         INTEGER NOT NULL DEFAULT 0,
    "ReceiverId"       INTEGER NOT NULL DEFAULT 0,
    "SenderRole"       TEXT NOT NULL DEFAULT '',
    "ReceiverRole"     TEXT NOT NULL DEFAULT '',
    "Subject"          TEXT NOT NULL DEFAULT '',
    "Content"          TEXT NOT NULL DEFAULT '',
    "IsRead"           BOOLEAN NOT NULL DEFAULT FALSE,
    "SentAt"           TIMESTAMP NOT NULL DEFAULT NOW(),
    "ReadAt"           TIMESTAMP,
    "ParentMessageId"  INTEGER,
    "AttachmentUrl"    TEXT
);

CREATE TABLE IF NOT EXISTS "GroupAnnouncements" (
    "AnnouncementId"  SERIAL PRIMARY KEY,
    "CreatorId"       INTEGER NOT NULL DEFAULT 0,
    "CreatorRole"     TEXT NOT NULL DEFAULT '',
    "Title"           TEXT NOT NULL DEFAULT '',
    "Content"         TEXT NOT NULL DEFAULT '',
    "TargetAudience"  TEXT NOT NULL DEFAULT '',
    "TargetFilter"    TEXT,
    "CreatedAt"       TIMESTAMP NOT NULL DEFAULT NOW(),
    "IsActive"        BOOLEAN NOT NULL DEFAULT TRUE
);

-- ==================== EXAMINATIONS ====================
CREATE TABLE IF NOT EXISTS "Exams" (
    "ExamId"              SERIAL PRIMARY KEY,
    "Title"               TEXT NOT NULL DEFAULT '',
    "Description"         TEXT NOT NULL DEFAULT '',
    "CourseId"            INTEGER NOT NULL DEFAULT 0,
    "CreatedByTeacherId"  INTEGER NOT NULL DEFAULT 0,
    "ScheduledDate"       TIMESTAMP NOT NULL,
    "Duration"            INTERVAL NOT NULL DEFAULT '0 seconds',
    "TotalMarks"          INTEGER NOT NULL DEFAULT 0,
    "PassingMarks"        INTEGER NOT NULL DEFAULT 0,
    "ExamType"            TEXT NOT NULL DEFAULT '',
    "IsPublished"         BOOLEAN NOT NULL DEFAULT FALSE,
    "CreatedAt"           TIMESTAMP NOT NULL DEFAULT NOW(),
    "PublishedAt"         TIMESTAMP
);

CREATE TABLE IF NOT EXISTS "ExamQuestions" (
    "QuestionId"    SERIAL PRIMARY KEY,
    "ExamId"        INTEGER NOT NULL DEFAULT 0,
    "QuestionText"  TEXT NOT NULL DEFAULT '',
    "QuestionType"  TEXT NOT NULL DEFAULT '',
    "Marks"         INTEGER NOT NULL DEFAULT 0,
    "OrderIndex"    INTEGER NOT NULL DEFAULT 0,
    "OptionA"       TEXT,
    "OptionB"       TEXT,
    "OptionC"       TEXT,
    "OptionD"       TEXT,
    "CorrectAnswer" TEXT
);

CREATE TABLE IF NOT EXISTS "ExamSubmissions" (
    "SubmissionId"   SERIAL PRIMARY KEY,
    "ExamId"         INTEGER NOT NULL DEFAULT 0,
    "StudentId"      INTEGER NOT NULL DEFAULT 0,
    "StartedAt"      TIMESTAMP NOT NULL,
    "SubmittedAt"    TIMESTAMP,
    "IsCompleted"    BOOLEAN NOT NULL DEFAULT FALSE,
    "ObtainedMarks"  INTEGER,
    "Status"         TEXT NOT NULL DEFAULT 'InProgress'
);

CREATE TABLE IF NOT EXISTS "ExamAnswers" (
    "AnswerId"       SERIAL PRIMARY KEY,
    "SubmissionId"   INTEGER NOT NULL DEFAULT 0,
    "QuestionId"     INTEGER NOT NULL DEFAULT 0,
    "StudentAnswer"  TEXT NOT NULL DEFAULT '',
    "MarksAwarded"   INTEGER,
    "IsCorrect"      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS "ExamResults" (
    "ResultId"              SERIAL PRIMARY KEY,
    "ExamId"                INTEGER NOT NULL DEFAULT 0,
    "StudentId"             INTEGER NOT NULL DEFAULT 0,
    "ObtainedMarks"         INTEGER NOT NULL DEFAULT 0,
    "TotalMarks"            INTEGER NOT NULL DEFAULT 0,
    "Percentage"            NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Grade"                 TEXT NOT NULL DEFAULT '',
    "IsPassed"              BOOLEAN NOT NULL DEFAULT FALSE,
    "EvaluatedAt"           TIMESTAMP NOT NULL,
    "EvaluatedByTeacherId"  INTEGER NOT NULL DEFAULT 0
);

-- ============================================================
-- SEED DATA: TimeSlots (30 slots)
-- ============================================================
INSERT INTO "TimeSlots" ("TimeSlotId","CourseId","TeacherId","DayOfWeek","StartTime","EndTime","Room","Semester","Year","IsActive","CreatedAt") VALUES
(1,  1,  1,  'Monday',    '09:00:00', '10:00:00', 'CS-301',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(2,  1,  1,  'Wednesday', '09:00:00', '10:00:00', 'CS-301',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(3,  2,  1,  'Monday',    '10:15:00', '11:15:00', 'CS-302',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(4,  2,  1,  'Thursday',  '10:15:00', '11:15:00', 'CS-302',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(5,  3,  2,  'Tuesday',   '09:00:00', '10:00:00', 'CS-Lab1',  3, 2024, TRUE, '2024-07-01 00:00:00'),
(6,  3,  2,  'Friday',    '09:00:00', '10:00:00', 'CS-Lab1',  3, 2024, TRUE, '2024-07-01 00:00:00'),
(7,  4,  1,  'Tuesday',   '11:30:00', '12:30:00', 'CS-303',   3, 2024, TRUE, '2024-07-01 00:00:00'),
(8,  5,  2,  'Wednesday', '11:30:00', '12:30:00', 'CS-304',   5, 2024, TRUE, '2024-07-01 00:00:00'),
(9,  6,  2,  'Thursday',  '14:00:00', '15:30:00', 'AI-Lab',   5, 2024, TRUE, '2024-07-01 00:00:00'),
(10, 7,  3,  'Monday',    '09:00:00', '10:00:00', 'IT-201',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(11, 7,  3,  'Wednesday', '09:00:00', '10:00:00', 'IT-201',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(12, 8,  3,  'Tuesday',   '10:15:00', '11:15:00', 'IT-202',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(13, 9,  4,  'Monday',    '11:30:00', '12:30:00', 'IT-Lab1',  3, 2024, TRUE, '2024-07-01 00:00:00'),
(14, 9,  4,  'Thursday',  '11:30:00', '12:30:00', 'IT-Lab1',  3, 2024, TRUE, '2024-07-01 00:00:00'),
(15, 10, 4,  'Friday',    '10:15:00', '11:15:00', 'IT-203',   3, 2024, TRUE, '2024-07-01 00:00:00'),
(16, 11, 5,  'Monday',    '14:00:00', '15:00:00', 'ECE-101',  1, 2024, TRUE, '2024-07-01 00:00:00'),
(17, 11, 5,  'Wednesday', '14:00:00', '15:00:00', 'ECE-101',  1, 2024, TRUE, '2024-07-01 00:00:00'),
(18, 12, 5,  'Tuesday',   '14:00:00', '15:00:00', 'ECE-Lab',  1, 2024, TRUE, '2024-07-01 00:00:00'),
(19, 14, 6,  'Monday',    '09:00:00', '10:00:00', 'ME-101',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(20, 14, 6,  'Wednesday', '10:15:00', '11:15:00', 'ME-101',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(21, 15, 6,  'Tuesday',   '09:00:00', '10:00:00', 'ME-Lab',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(22, 16, 7,  'Thursday',  '14:00:00', '15:00:00', 'CE-101',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(23, 17, 7,  'Friday',    '14:00:00', '15:30:00', 'CE-Lab',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(24, 18, 8,  'Monday',    '10:15:00', '11:15:00', 'EE-101',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(25, 18, 8,  'Wednesday', '10:15:00', '11:15:00', 'EE-101',   1, 2024, TRUE, '2024-07-01 00:00:00'),
(26, 19, 9,  'Tuesday',   '11:30:00', '12:30:00', 'MATH-101', 1, 2024, TRUE, '2024-07-01 00:00:00'),
(27, 19, 9,  'Thursday',  '11:30:00', '12:30:00', 'MATH-101', 1, 2024, TRUE, '2024-07-01 00:00:00'),
(28, 20, 10, 'Friday',    '09:00:00', '10:00:00', 'PHY-101',  1, 2024, TRUE, '2024-07-01 00:00:00'),
(29, 20, 10, 'Wednesday', '14:00:00', '15:00:00', 'PHY-Lab',  1, 2024, TRUE, '2024-07-01 00:00:00'),
(30, 13, 5,  'Thursday',  '09:00:00', '10:00:00', 'ECE-201',  3, 2024, TRUE, '2024-07-01 00:00:00')
ON CONFLICT ("TimeSlotId") DO NOTHING;

-- ============================================================
-- SEED DATA: Notices (10)
-- ============================================================
INSERT INTO "Notices" ("NoticeId","Title","Content","Category","TargetRole","CreatedByUserId","CreatedByName","IsActive","CreatedAt") VALUES
(1,  'Welcome to Semester 2 — 2025',    'Dear students, welcome back! Classes begin on Jan 20.',                           'Academic', NULL,      1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(2,  'Mid-Semester Exam Schedule',       'Mid-semester exams: March 10-20. Check timetable.',                               'Exam',     'Student', 1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(3,  'Annual Sports Day',               'Annual sports day on Feb 28. All are welcome!',                                   'Event',    NULL,      1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(4,  'Library Extended Hours',           'Library open until 10 PM during exam season.',                                    'General',  NULL,      1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(5,  'Faculty Meeting',                 'Monthly faculty meeting on March 1 at 3 PM.',                                     'General',  'Teacher', 1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(6,  'Scholarship Applications Open',   'Merit scholarship applications for 2025 now open. Deadline: March 15.',           'Academic', 'Student', 1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(7,  'Hackathon 2025',                  '24-hour hackathon on March 25. Register at CS dept.',                              'Event',    'Student', 1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(8,  'New Lab Equipment',               'New IoT lab equipment arrived. Training starts March 5.',                          'Academic', 'Teacher', 1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(9,  'Holiday Notice — Holi',           'College closed on March 14 for Holi.',                                            'General',  NULL,      1, 'Admin', TRUE, '2025-02-01 00:00:00'),
(10, 'Placement Drive — TCS',           'TCS campus placement on April 5. Eligibility: 7+ CGPA.',                          'Academic', 'Student', 1, 'Admin', TRUE, '2025-02-01 00:00:00')
ON CONFLICT ("NoticeId") DO NOTHING;

-- Reset sequences
SELECT setval(pg_get_serial_sequence('"TimeSlots"', 'TimeSlotId'), (SELECT COALESCE(MAX("TimeSlotId"), 1) FROM "TimeSlots"));
SELECT setval(pg_get_serial_sequence('"Notices"', 'NoticeId'), (SELECT COALESCE(MAX("NoticeId"), 1) FROM "Notices"));
