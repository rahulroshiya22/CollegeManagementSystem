-- ============================================================
-- SUPABASE — COMPLETE CMS DATABASE SETUP
-- ============================================================
-- Run this ENTIRE script in Supabase SQL Editor (one go)
-- This creates 8 schemas (one per microservice) with all
-- tables, indexes, and seed data.
-- ============================================================

-- ==================== CREATE SCHEMAS ====================
CREATE SCHEMA IF NOT EXISTS cms_auth;
CREATE SCHEMA IF NOT EXISTS cms_students;
CREATE SCHEMA IF NOT EXISTS cms_courses;
CREATE SCHEMA IF NOT EXISTS cms_enrollments;
CREATE SCHEMA IF NOT EXISTS cms_fees;
CREATE SCHEMA IF NOT EXISTS cms_attendance;
CREATE SCHEMA IF NOT EXISTS cms_academic;
CREATE SCHEMA IF NOT EXISTS cms_chat;

-- ============================================================
-- 1. CMS_AUTH SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_auth."Users" (
    "UserId"              SERIAL PRIMARY KEY,
    "Email"               VARCHAR(255) NOT NULL,
    "PasswordHash"        TEXT,
    "FirstName"           VARCHAR(100) NOT NULL,
    "LastName"            VARCHAR(100) NOT NULL,
    "Role"                INTEGER NOT NULL DEFAULT 0,
    "IsActive"            BOOLEAN NOT NULL DEFAULT TRUE,
    "GoogleId"            VARCHAR(255),
    "ProfilePictureUrl"   VARCHAR(500),
    "PhotoUrl"            VARCHAR(500),
    "AuthProvider"        INTEGER NOT NULL DEFAULT 0,
    "RefreshToken"        VARCHAR(500),
    "RefreshTokenExpiry"  TIMESTAMP,
    "CreatedAt"           TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"           TIMESTAMP,
    "StudentId"           INTEGER,
    "TeacherId"           INTEGER
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON cms_auth."Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_GoogleId" ON cms_auth."Users" ("GoogleId");

CREATE TABLE IF NOT EXISTS cms_auth."Teachers" (
    "TeacherId"       SERIAL PRIMARY KEY,
    "UserId"          INTEGER NOT NULL REFERENCES cms_auth."Users"("UserId") ON DELETE CASCADE,
    "Department"      VARCHAR(100) NOT NULL,
    "Specialization"  VARCHAR(200) NOT NULL,
    "Qualification"   VARCHAR(200),
    "Experience"      INTEGER NOT NULL DEFAULT 0,
    "PhoneNumber"     VARCHAR(20),
    "IsActive"        BOOLEAN NOT NULL DEFAULT TRUE,
    "JoiningDate"     TIMESTAMP NOT NULL DEFAULT NOW(),
    "CreatedAt"       TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"       TIMESTAMP
);

-- ============================================================
-- 2. CMS_STUDENTS SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_students."Departments" (
    "DepartmentId"  SERIAL PRIMARY KEY,
    "Name"          VARCHAR(200) NOT NULL,
    "Code"          VARCHAR(10) NOT NULL,
    "Description"   TEXT,
    "IsActive"      BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Stu_Departments_Code" ON cms_students."Departments" ("Code");

CREATE TABLE IF NOT EXISTS cms_students."Students" (
    "StudentId"      SERIAL PRIMARY KEY,
    "FirstName"      VARCHAR(100) NOT NULL,
    "LastName"       VARCHAR(100) NOT NULL,
    "Email"          VARCHAR(200) NOT NULL,
    "Phone"          TEXT NOT NULL DEFAULT '',
    "RollNumber"     VARCHAR(50) NOT NULL,
    "DateOfBirth"    TIMESTAMP NOT NULL,
    "Gender"         TEXT NOT NULL DEFAULT '',
    "Address"        TEXT NOT NULL DEFAULT '',
    "DepartmentId"   INTEGER NOT NULL REFERENCES cms_students."Departments"("DepartmentId") ON DELETE RESTRICT,
    "AdmissionYear"  INTEGER NOT NULL DEFAULT 0,
    "Status"         TEXT NOT NULL DEFAULT 'Active',
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"      TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_Email" ON cms_students."Students" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_RollNumber" ON cms_students."Students" ("RollNumber");

-- ============================================================
-- 3. CMS_COURSES SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_courses."Departments" (
    "DepartmentId"  SERIAL PRIMARY KEY,
    "Name"          TEXT NOT NULL DEFAULT '',
    "Code"          TEXT NOT NULL DEFAULT '',
    "IsActive"      BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Crs_Departments_Code" ON cms_courses."Departments" ("Code");

CREATE TABLE IF NOT EXISTS cms_courses."Courses" (
    "CourseId"       SERIAL PRIMARY KEY,
    "CourseCode"     VARCHAR(20) NOT NULL,
    "CourseName"     VARCHAR(200) NOT NULL,
    "Description"    TEXT NOT NULL DEFAULT '',
    "Credits"        INTEGER NOT NULL DEFAULT 0,
    "Semester"       INTEGER NOT NULL DEFAULT 0,
    "DepartmentId"   INTEGER NOT NULL REFERENCES cms_courses."Departments"("DepartmentId"),
    "IsActive"       BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Courses_CourseCode" ON cms_courses."Courses" ("CourseCode");

-- ============================================================
-- 4. CMS_ENROLLMENTS SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_enrollments."Enrollments" (
    "EnrollmentId"    SERIAL PRIMARY KEY,
    "StudentId"       INTEGER NOT NULL,
    "CourseId"        INTEGER NOT NULL,
    "EnrollmentDate"  TIMESTAMP NOT NULL DEFAULT NOW(),
    "Semester"        INTEGER NOT NULL DEFAULT 0,
    "Year"            INTEGER NOT NULL DEFAULT 0,
    "Status"          TEXT NOT NULL DEFAULT 'Active',
    "Grade"           NUMERIC(18,2)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Enrollments_Student_Course_Year_Sem"
    ON cms_enrollments."Enrollments" ("StudentId", "CourseId", "Year", "Semester");

-- ============================================================
-- 5. CMS_FEES SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_fees."Fees" (
    "FeeId"        SERIAL PRIMARY KEY,
    "StudentId"    INTEGER NOT NULL,
    "Amount"       NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Description"  TEXT NOT NULL DEFAULT '',
    "Status"       TEXT NOT NULL DEFAULT 'Pending',
    "DueDate"      TIMESTAMP NOT NULL,
    "PaidDate"     TIMESTAMP,
    "CreatedAt"    TIMESTAMP NOT NULL DEFAULT NOW()
);

-- ============================================================
-- 6. CMS_ATTENDANCE SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_attendance."Attendances" (
    "AttendanceId"  SERIAL PRIMARY KEY,
    "StudentId"     INTEGER NOT NULL,
    "CourseId"      INTEGER NOT NULL,
    "Date"          TIMESTAMP NOT NULL,
    "IsPresent"     BOOLEAN NOT NULL DEFAULT FALSE,
    "Remarks"       TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Attendances_Student_Course_Date"
    ON cms_attendance."Attendances" ("StudentId", "CourseId", "Date");

-- ============================================================
-- 7. CMS_ACADEMIC SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_academic."TimeSlots" (
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

CREATE TABLE IF NOT EXISTS cms_academic."Grades" (
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
    ON cms_academic."Grades" ("StudentId", "CourseId", "Semester", "Year");

CREATE TABLE IF NOT EXISTS cms_academic."Notices" (
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

CREATE TABLE IF NOT EXISTS cms_academic."Messages" (
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

CREATE TABLE IF NOT EXISTS cms_academic."GroupAnnouncements" (
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

CREATE TABLE IF NOT EXISTS cms_academic."Exams" (
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

CREATE TABLE IF NOT EXISTS cms_academic."ExamQuestions" (
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

CREATE TABLE IF NOT EXISTS cms_academic."ExamSubmissions" (
    "SubmissionId"   SERIAL PRIMARY KEY,
    "ExamId"         INTEGER NOT NULL DEFAULT 0,
    "StudentId"      INTEGER NOT NULL DEFAULT 0,
    "StartedAt"      TIMESTAMP NOT NULL,
    "SubmittedAt"    TIMESTAMP,
    "IsCompleted"    BOOLEAN NOT NULL DEFAULT FALSE,
    "ObtainedMarks"  INTEGER,
    "Status"         TEXT NOT NULL DEFAULT 'InProgress'
);

CREATE TABLE IF NOT EXISTS cms_academic."ExamAnswers" (
    "AnswerId"       SERIAL PRIMARY KEY,
    "SubmissionId"   INTEGER NOT NULL DEFAULT 0,
    "QuestionId"     INTEGER NOT NULL DEFAULT 0,
    "StudentAnswer"  TEXT NOT NULL DEFAULT '',
    "MarksAwarded"   INTEGER,
    "IsCorrect"      BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE TABLE IF NOT EXISTS cms_academic."ExamResults" (
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
-- 8. CMS_CHAT SCHEMA
-- ============================================================

CREATE TABLE IF NOT EXISTS cms_chat."Conversations" (
    "Id"             SERIAL PRIMARY KEY,
    "StudentId"      INTEGER NOT NULL,
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW(),
    "LastMessageAt"  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS cms_chat."ChatMessages" (
    "Id"              SERIAL PRIMARY KEY,
    "ConversationId"  INTEGER NOT NULL REFERENCES cms_chat."Conversations"("Id") ON DELETE CASCADE,
    "Role"            VARCHAR(20) NOT NULL,
    "Message"         TEXT NOT NULL,
    "Timestamp"       TIMESTAMP NOT NULL DEFAULT NOW(),
    "ServiceCalled"   TEXT
);


-- ============================================================
-- ============================================================
--                    S E E D   D A T A
-- ============================================================
-- ============================================================

-- ==================== DEPARTMENTS (Students DB) ====================
INSERT INTO cms_students."Departments" ("DepartmentId", "Name", "Code", "Description") VALUES
(1, 'Computer Science',       'CS',   'Computer Science and Engineering'),
(2, 'Information Technology',  'IT',   'Information Technology'),
(3, 'Electronics',            'ECE',  'Electronics and Communication Engineering'),
(4, 'Mechanical',             'ME',   'Mechanical Engineering'),
(5, 'Civil',                  'CE',   'Civil Engineering'),
(6, 'Electrical',             'EE',   'Electrical Engineering'),
(7, 'Mathematics',            'MATH', 'Applied Mathematics'),
(8, 'Physics',                'PHY',  'Applied Physics')
ON CONFLICT ("DepartmentId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_students."Departments"', 'DepartmentId'), (SELECT COALESCE(MAX("DepartmentId"), 1) FROM cms_students."Departments"));

-- ==================== DEPARTMENTS (Courses DB) ====================
INSERT INTO cms_courses."Departments" ("DepartmentId", "Name", "Code") VALUES
(1, 'Computer Science',       'CS'),
(2, 'Information Technology',  'IT'),
(3, 'Electronics',            'ECE'),
(4, 'Mechanical',             'ME'),
(5, 'Civil',                  'CE'),
(6, 'Electrical',             'EE'),
(7, 'Mathematics',            'MATH'),
(8, 'Physics',                'PHY')
ON CONFLICT ("DepartmentId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_courses."Departments"', 'DepartmentId'), (SELECT COALESCE(MAX("DepartmentId"), 1) FROM cms_courses."Departments"));

-- ==================== STUDENTS ====================
INSERT INTO cms_students."Students" ("StudentId","FirstName","LastName","Email","Phone","RollNumber","DateOfBirth","Gender","Address","DepartmentId","AdmissionYear","Status","CreatedAt") VALUES
(1,'Aarav','Sharma','aarav.sharma@cms.com','9876500001','CS2024001','2004-03-15','Male','12 MG Road, Mumbai',1,2024,'Active','2024-07-01'),
(2,'Vivaan','Patel','vivaan.patel@cms.com','9876500002','CS2024002','2004-06-22','Male','45 Park Street, Ahmedabad',1,2024,'Active','2024-07-01'),
(3,'Aditya','Reddy','aditya.reddy@cms.com','9876500003','CS2024003','2004-01-10','Male','78 Jubilee Hills, Hyderabad',1,2024,'Active','2024-07-01'),
(4,'Sai','Kumar','sai.kumar@cms.com','9876500004','CS2024004','2004-09-05','Male','23 Banjara Hills, Hyderabad',1,2024,'Active','2024-07-01'),
(5,'Arjun','Singh','arjun.singh@cms.com','9876500005','CS2024005','2004-04-18','Male','56 Connaught Place, Delhi',1,2024,'Active','2024-07-01'),
(6,'Diya','Gupta','diya.gupta@cms.com','9876500006','CS2024006','2004-11-30','Female','89 Salt Lake, Kolkata',1,2024,'Active','2024-07-01'),
(7,'Ananya','Iyer','ananya.iyer@cms.com','9876500007','CS2024007','2004-07-25','Female','34 T Nagar, Chennai',1,2024,'Active','2024-07-01'),
(8,'Ishaan','Joshi','ishaan.joshi@cms.com','9876500008','IT2024001','2004-02-14','Male','67 FC Road, Pune',2,2024,'Active','2024-07-01'),
(9,'Kabir','Mehta','kabir.mehta@cms.com','9876500009','IT2024002','2004-05-09','Male','90 SG Highway, Ahmedabad',2,2024,'Active','2024-07-01'),
(10,'Reyansh','Desai','reyansh.desai@cms.com','9876500010','IT2024003','2004-08-20','Male','12 Marine Drive, Mumbai',2,2024,'Active','2024-07-01'),
(11,'Priya','Nair','priya.nair@cms.com','9876500011','IT2024004','2004-12-01','Female','45 MG Road, Kochi',2,2024,'Active','2024-07-01'),
(12,'Saanvi','Rao','saanvi.rao@cms.com','9876500012','IT2024005','2004-03-28','Female','78 Koramangala, Bangalore',2,2024,'Active','2024-07-01'),
(13,'Myra','Choudhury','myra.choudhury@cms.com','9876500013','IT2024006','2004-10-15','Female','23 Park Circus, Kolkata',2,2024,'Active','2024-07-01'),
(14,'Rohan','Verma','rohan.verma@cms.com','9876500014','ECE2024001','2004-06-11','Male','56 Hazratganj, Lucknow',3,2024,'Active','2024-07-01'),
(15,'Vihaan','Mishra','vihaan.mishra@cms.com','9876500015','ECE2024002','2004-01-24','Male','89 Civil Lines, Jaipur',3,2024,'Active','2024-07-01'),
(16,'Advika','Pandey','advika.pandey@cms.com','9876500016','ECE2024003','2004-04-07','Female','34 Gomti Nagar, Lucknow',3,2024,'Active','2024-07-01'),
(17,'Kiara','Saxena','kiara.saxena@cms.com','9876500017','ECE2024004','2004-09-19','Female','67 Aundh, Pune',3,2024,'Active','2024-07-01'),
(18,'Rudra','Chauhan','rudra.chauhan@cms.com','9876500018','ECE2024005','2004-02-28','Male','90 Satellite, Ahmedabad',3,2024,'Active','2024-07-01'),
(19,'Dev','Tiwari','dev.tiwari@cms.com','9876500019','ME2024001','2004-07-03','Male','12 Rajouri Garden, Delhi',4,2024,'Active','2024-07-01'),
(20,'Arnav','Kapoor','arnav.kapoor@cms.com','9876500020','ME2024002','2004-11-16','Male','45 Model Town, Ludhiana',4,2024,'Active','2024-07-01'),
(21,'Dhruv','Malhotra','dhruv.malhotra@cms.com','9876500021','ME2024003','2004-05-22','Male','78 Sector 17, Chandigarh',4,2024,'Active','2024-07-01'),
(22,'Kavya','Bhatt','kavya.bhatt@cms.com','9876500022','ME2024004','2004-08-08','Female','23 Ellis Bridge, Ahmedabad',4,2024,'Active','2024-07-01'),
(23,'Riya','Agarwal','riya.agarwal@cms.com','9876500023','ME2024005','2004-12-25','Female','56 GS Road, Guwahati',4,2024,'Active','2024-07-01'),
(24,'Yash','Kulkarni','yash.kulkarni@cms.com','9876500024','CE2024001','2004-03-11','Male','89 Deccan, Pune',5,2024,'Active','2024-07-01'),
(25,'Om','Jain','om.jain@cms.com','9876500025','CE2024002','2004-06-05','Male','34 Vaishali Nagar, Jaipur',5,2024,'Active','2024-07-01'),
(26,'Anika','Shetty','anika.shetty@cms.com','9876500026','CE2024003','2004-09-29','Female','67 Bandra, Mumbai',5,2024,'Active','2024-07-01'),
(27,'Zara','Khan','zara.khan@cms.com','9876500027','CE2024004','2004-01-17','Female','90 Lajpat Nagar, Delhi',5,2024,'Active','2024-07-01'),
(28,'Ayaan','Bose','ayaan.bose@cms.com','9876500028','EE2024001','2004-04-30','Male','12 Ballygunge, Kolkata',6,2024,'Active','2024-07-01'),
(29,'Tanvi','Das','tanvi.das@cms.com','9876500029','EE2024002','2004-10-08','Female','45 Adyar, Chennai',6,2024,'Active','2024-07-01'),
(30,'Krish','Menon','krish.menon@cms.com','9876500030','EE2024003','2004-02-20','Male','78 Vyttila, Kochi',6,2024,'Active','2024-07-01'),
(31,'Nisha','Pillai','nisha.pillai@cms.com','9876500031','EE2024004','2004-07-14','Female','23 Technopark, Trivandrum',6,2024,'Active','2024-07-01'),
(32,'Parth','Sawant','parth.sawant@cms.com','9876500032','EE2024005','2004-11-03','Male','56 Shivaji Nagar, Pune',6,2024,'Active','2024-07-01'),
(33,'Mira','Hegde','mira.hegde@cms.com','9876500033','MATH2024001','2004-05-27','Female','89 Indiranagar, Bangalore',7,2024,'Active','2024-07-01'),
(34,'Harsh','Gowda','harsh.gowda@cms.com','9876500034','MATH2024002','2004-08-13','Male','34 Whitefield, Bangalore',7,2024,'Active','2024-07-01'),
(35,'Pooja','Yadav','pooja.yadav@cms.com','9876500035','MATH2024003','2004-12-06','Female','67 Dwarka, Delhi',7,2024,'Active','2024-07-01'),
(36,'Rahul','Thakur','rahul.thakur@cms.com','9876500036','MATH2024004','2004-03-24','Male','90 Shimla Road, Chandigarh',7,2024,'Active','2024-07-01'),
(37,'Sneha','Prasad','sneha.prasad@cms.com','9876500037','PHY2024001','2004-06-18','Female','12 Gandhi Maidan, Patna',8,2024,'Active','2024-07-01'),
(38,'Vikram','Rathore','vikram.rathore@cms.com','9876500038','PHY2024002','2004-09-02','Male','45 Ajmer Road, Jaipur',8,2024,'Active','2024-07-01'),
(39,'Neha','Dubey','neha.dubey@cms.com','9876500039','PHY2024003','2004-01-29','Female','78 Mahanagar, Lucknow',8,2024,'Active','2024-07-01'),
(40,'Arun','Nambiar','arun.nambiar@cms.com','9876500040','PHY2024004','2004-04-12','Male','23 Palarivattom, Kochi',8,2024,'Active','2024-07-01'),
(41,'Lakshmi','Sundaram','lakshmi.sundaram@cms.com','9876500041','CS2023001','2003-07-08','Female','56 Anna Nagar, Chennai',1,2023,'Active','2023-07-01'),
(42,'Manish','Chandra','manish.chandra@cms.com','9876500042','CS2023002','2003-10-21','Male','89 Malviya Nagar, Delhi',1,2023,'Active','2023-07-01'),
(43,'Divya','Krishnan','divya.krishnan@cms.com','9876500043','IT2023001','2003-02-15','Female','34 Kothrud, Pune',2,2023,'Active','2023-07-01'),
(44,'Suresh','Babu','suresh.babu@cms.com','9876500044','IT2023002','2003-05-30','Male','67 HSR Layout, Bangalore',2,2023,'Active','2023-07-01'),
(45,'Anjali','Srivastava','anjali.srivastava@cms.com','9876500045','ECE2023001','2003-08-19','Female','90 Vibhuti Khand, Lucknow',3,2023,'Active','2023-07-01'),
(46,'Kunal','Roy','kunal.roy@cms.com','9876500046','ME2023001','2003-11-04','Male','12 Gariahat, Kolkata',4,2023,'Active','2023-07-01'),
(47,'Shreya','Banerjee','shreya.banerjee@cms.com','9876500047','CE2023001','2003-03-17','Female','45 Lake Town, Kolkata',5,2023,'Active','2023-07-01'),
(48,'Raj','Chopra','raj.chopra@cms.com','9876500048','EE2023001','2003-06-26','Male','78 Sector 44, Gurgaon',6,2023,'Active','2023-07-01'),
(49,'Meera','Venkatesh','meera.venkatesh@cms.com','9876500049','MATH2023001','2003-09-10','Female','23 JP Nagar, Bangalore',7,2023,'Active','2023-07-01'),
(50,'Aakash','Pandey','aakash.pandey@cms.com','9876500050','PHY2023001','2003-12-28','Male','56 Ashok Nagar, Bhopal',8,2023,'Active','2023-07-01')
ON CONFLICT ("StudentId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_students."Students"', 'StudentId'), (SELECT COALESCE(MAX("StudentId"), 1) FROM cms_students."Students"));

-- ==================== COURSES ====================
INSERT INTO cms_courses."Courses" ("CourseId","CourseCode","CourseName","Description","Credits","Semester","DepartmentId","IsActive","CreatedAt") VALUES
(1,'CS101','Data Structures','Fundamental data structures and algorithms',4,1,1,TRUE,'2024-01-01'),
(2,'CS102','Object Oriented Programming','OOP concepts with C++ and Java',4,1,1,TRUE,'2024-01-01'),
(3,'CS201','Database Management Systems','Relational databases and SQL',4,3,1,TRUE,'2024-01-01'),
(4,'CS202','Operating Systems','Process management, memory, file systems',4,3,1,TRUE,'2024-01-01'),
(5,'CS301','Computer Networks','Network protocols and architecture',3,5,1,TRUE,'2024-01-01'),
(6,'CS302','Machine Learning','Supervised and unsupervised learning',4,5,1,TRUE,'2024-01-01'),
(7,'IT101','Web Technologies','HTML, CSS, JavaScript, React',4,1,2,TRUE,'2024-01-01'),
(8,'IT102','Software Engineering','SDLC, Agile, Design Patterns',3,1,2,TRUE,'2024-01-01'),
(9,'IT201','Cloud Computing','AWS, Azure, Docker, Kubernetes',4,3,2,TRUE,'2024-01-01'),
(10,'IT202','Cyber Security','Network security and cryptography',3,3,2,TRUE,'2024-01-01'),
(11,'ECE101','Digital Electronics','Logic gates, combinational circuits',4,1,3,TRUE,'2024-01-01'),
(12,'ECE102','Signals and Systems','Signal processing fundamentals',4,1,3,TRUE,'2024-01-01'),
(13,'ECE201','Microprocessors','8085/8086 architecture and programming',4,3,3,TRUE,'2024-01-01'),
(14,'ME101','Engineering Mechanics','Statics and dynamics',4,1,4,TRUE,'2024-01-01'),
(15,'ME102','Thermodynamics','Laws of thermodynamics and applications',4,1,4,TRUE,'2024-01-01'),
(16,'CE101','Surveying','Land surveying techniques',3,1,5,TRUE,'2024-01-01'),
(17,'CE102','Structural Analysis','Analysis of beams, trusses, frames',4,1,5,TRUE,'2024-01-01'),
(18,'EE101','Circuit Theory','Kirchhoffs laws, network theorems',4,1,6,TRUE,'2024-01-01'),
(19,'MATH101','Linear Algebra','Matrices, vector spaces, eigenvalues',3,1,7,TRUE,'2024-01-01'),
(20,'PHY101','Engineering Physics','Optics, quantum mechanics, solid state',3,1,8,TRUE,'2024-01-01')
ON CONFLICT ("CourseId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_courses."Courses"', 'CourseId'), (SELECT COALESCE(MAX("CourseId"), 1) FROM cms_courses."Courses"));

-- ==================== ENROLLMENTS ====================
INSERT INTO cms_enrollments."Enrollments" ("EnrollmentId","StudentId","CourseId","EnrollmentDate","Semester","Year","Status","Grade") VALUES
(1,1,1,'2024-07-15',1,2024,'Active',NULL),(2,1,2,'2024-07-15',1,2024,'Active',NULL),
(3,2,1,'2024-07-15',1,2024,'Active',NULL),(4,2,2,'2024-07-15',1,2024,'Active',NULL),
(5,3,1,'2024-07-15',1,2024,'Active',NULL),(6,3,3,'2024-07-15',1,2024,'Active',NULL),
(7,4,1,'2024-07-15',1,2024,'Active',NULL),(8,4,2,'2024-07-15',1,2024,'Active',NULL),
(9,5,1,'2024-07-15',1,2024,'Active',NULL),(10,5,3,'2024-07-15',1,2024,'Active',NULL),
(11,6,1,'2024-07-15',1,2024,'Active',NULL),(12,6,2,'2024-07-15',1,2024,'Active',NULL),
(13,7,1,'2024-07-15',1,2024,'Active',NULL),(14,7,3,'2024-07-15',1,2024,'Active',NULL),
(15,8,7,'2024-07-15',1,2024,'Active',NULL),(16,8,8,'2024-07-15',1,2024,'Active',NULL),
(17,9,7,'2024-07-15',1,2024,'Active',NULL),(18,9,8,'2024-07-15',1,2024,'Active',NULL),
(19,10,7,'2024-07-15',1,2024,'Active',NULL),(20,10,9,'2024-07-15',1,2024,'Active',NULL),
(21,11,7,'2024-07-15',1,2024,'Active',NULL),(22,11,8,'2024-07-15',1,2024,'Active',NULL),
(23,12,7,'2024-07-15',1,2024,'Active',NULL),(24,12,10,'2024-07-15',1,2024,'Active',NULL),
(25,13,7,'2024-07-15',1,2024,'Active',NULL),(26,13,9,'2024-07-15',1,2024,'Active',NULL),
(27,14,11,'2024-07-15',1,2024,'Active',NULL),(28,14,12,'2024-07-15',1,2024,'Active',NULL),
(29,15,11,'2024-07-15',1,2024,'Active',NULL),(30,15,12,'2024-07-15',1,2024,'Active',NULL),
(31,16,11,'2024-07-15',1,2024,'Active',NULL),(32,16,13,'2024-07-15',1,2024,'Active',NULL),
(33,17,11,'2024-07-15',1,2024,'Active',NULL),(34,17,12,'2024-07-15',1,2024,'Active',NULL),
(35,18,11,'2024-07-15',1,2024,'Active',NULL),(36,18,13,'2024-07-15',1,2024,'Active',NULL),
(37,19,14,'2024-07-15',1,2024,'Active',NULL),(38,19,15,'2024-07-15',1,2024,'Active',NULL),
(39,20,14,'2024-07-15',1,2024,'Active',NULL),(40,20,15,'2024-07-15',1,2024,'Active',NULL),
(41,21,14,'2024-07-15',1,2024,'Active',NULL),(42,22,15,'2024-07-15',1,2024,'Active',NULL),
(43,23,14,'2024-07-15',1,2024,'Active',NULL),(44,24,16,'2024-07-15',1,2024,'Active',NULL),
(45,24,17,'2024-07-15',1,2024,'Active',NULL),(46,25,16,'2024-07-15',1,2024,'Active',NULL),
(47,26,17,'2024-07-15',1,2024,'Active',NULL),(48,27,16,'2024-07-15',1,2024,'Active',NULL),
(49,28,18,'2024-07-15',1,2024,'Active',NULL),(50,29,18,'2024-07-15',1,2024,'Active',NULL),
(51,30,18,'2024-07-15',1,2024,'Active',NULL),(52,31,18,'2024-07-15',1,2024,'Active',NULL),
(53,32,18,'2024-07-15',1,2024,'Active',NULL),(54,33,19,'2024-07-15',1,2024,'Active',NULL),
(55,34,19,'2024-07-15',1,2024,'Active',NULL),(56,35,19,'2024-07-15',1,2024,'Active',NULL),
(57,36,19,'2024-07-15',1,2024,'Active',NULL),(58,37,20,'2024-07-15',1,2024,'Active',NULL),
(59,38,20,'2024-07-15',1,2024,'Active',NULL),(60,39,20,'2024-07-15',1,2024,'Active',NULL),
(61,40,20,'2024-07-15',1,2024,'Active',NULL),(62,41,1,'2023-07-15',1,2023,'Active',NULL),
(63,41,3,'2024-01-15',3,2024,'Active',NULL),(64,42,1,'2023-07-15',1,2023,'Active',NULL),
(65,42,4,'2024-01-15',3,2024,'Active',NULL),(66,43,7,'2023-07-15',1,2023,'Active',NULL),
(67,43,9,'2024-01-15',3,2024,'Active',NULL),(68,44,7,'2023-07-15',1,2023,'Active',NULL),
(69,44,10,'2024-01-15',3,2024,'Active',NULL),(70,45,11,'2023-07-15',1,2023,'Active',NULL),
(71,46,14,'2023-07-15',1,2023,'Active',NULL),(72,47,16,'2023-07-15',1,2023,'Active',NULL),
(73,48,18,'2023-07-15',1,2023,'Active',NULL),(74,49,19,'2023-07-15',1,2023,'Active',NULL),
(75,50,20,'2023-07-15',1,2023,'Active',NULL)
ON CONFLICT ("EnrollmentId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_enrollments."Enrollments"', 'EnrollmentId'), (SELECT COALESCE(MAX("EnrollmentId"), 1) FROM cms_enrollments."Enrollments"));

-- ==================== FEES ====================
INSERT INTO cms_fees."Fees" ("FeeId","StudentId","Amount","Description","Status","DueDate","PaidDate","CreatedAt") VALUES
(1,1,45000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-28','2024-07-01'),
(2,2,45000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-30','2024-07-01'),
(3,3,45000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-08-01','2024-07-01'),
(4,4,45000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(5,5,45000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-25','2024-07-01'),
(6,6,45000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-29','2024-07-01'),
(7,7,45000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(8,8,42000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-20','2024-07-01'),
(9,9,42000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-08-01','2024-07-01'),
(10,10,42000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(11,11,42000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-22','2024-07-01'),
(12,12,42000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-31','2024-07-01'),
(13,13,42000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(14,14,40000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-27','2024-07-01'),
(15,15,40000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-26','2024-07-01'),
(16,16,40000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(17,17,40000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-08-01','2024-07-01'),
(18,18,40000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-24','2024-07-01'),
(19,19,38000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-19','2024-07-01'),
(20,20,38000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(21,21,38000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-23','2024-07-01'),
(22,22,38000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-30','2024-07-01'),
(23,23,38000,'Tuition Fee - Sem 1','Pending','2024-08-01',NULL,'2024-07-01'),
(24,24,36000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-21','2024-07-01'),
(25,25,36000,'Tuition Fee - Sem 1','Paid','2024-08-01','2024-07-28','2024-07-01'),
(26,1,5000,'Lab Fee - Sem 1','Paid','2024-08-15','2024-08-10','2024-07-01'),
(27,2,5000,'Lab Fee - Sem 1','Paid','2024-08-15','2024-08-12','2024-07-01'),
(28,3,5000,'Lab Fee - Sem 1','Pending','2024-08-15',NULL,'2024-07-01'),
(29,8,5000,'Lab Fee - Sem 1','Paid','2024-08-15','2024-08-05','2024-07-01'),
(30,14,4000,'Lab Fee - Sem 1','Paid','2024-08-15','2024-08-08','2024-07-01'),
(31,1,45000,'Tuition Fee - Sem 2','Pending','2025-01-15',NULL,'2024-12-01'),
(32,2,45000,'Tuition Fee - Sem 2','Pending','2025-01-15',NULL,'2024-12-01'),
(33,3,45000,'Tuition Fee - Sem 2','Paid','2025-01-15','2025-01-10','2024-12-01'),
(34,8,42000,'Tuition Fee - Sem 2','Paid','2025-01-15','2025-01-12','2024-12-01'),
(35,14,40000,'Tuition Fee - Sem 2','Pending','2025-01-15',NULL,'2024-12-01')
ON CONFLICT ("FeeId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_fees."Fees"', 'FeeId'), (SELECT COALESCE(MAX("FeeId"), 1) FROM cms_fees."Fees"));

-- ==================== TIMESLOTS ====================
INSERT INTO cms_academic."TimeSlots" ("TimeSlotId","CourseId","TeacherId","DayOfWeek","StartTime","EndTime","Room","Semester","Year","IsActive","CreatedAt") VALUES
(1,1,1,'Monday','09:00:00','10:00:00','CS-301',1,2024,TRUE,'2024-07-01'),
(2,1,1,'Wednesday','09:00:00','10:00:00','CS-301',1,2024,TRUE,'2024-07-01'),
(3,2,1,'Monday','10:15:00','11:15:00','CS-302',1,2024,TRUE,'2024-07-01'),
(4,2,1,'Thursday','10:15:00','11:15:00','CS-302',1,2024,TRUE,'2024-07-01'),
(5,3,2,'Tuesday','09:00:00','10:00:00','CS-Lab1',3,2024,TRUE,'2024-07-01'),
(6,3,2,'Friday','09:00:00','10:00:00','CS-Lab1',3,2024,TRUE,'2024-07-01'),
(7,4,1,'Tuesday','11:30:00','12:30:00','CS-303',3,2024,TRUE,'2024-07-01'),
(8,5,2,'Wednesday','11:30:00','12:30:00','CS-304',5,2024,TRUE,'2024-07-01'),
(9,6,2,'Thursday','14:00:00','15:30:00','AI-Lab',5,2024,TRUE,'2024-07-01'),
(10,7,3,'Monday','09:00:00','10:00:00','IT-201',1,2024,TRUE,'2024-07-01'),
(11,7,3,'Wednesday','09:00:00','10:00:00','IT-201',1,2024,TRUE,'2024-07-01'),
(12,8,3,'Tuesday','10:15:00','11:15:00','IT-202',1,2024,TRUE,'2024-07-01'),
(13,9,4,'Monday','11:30:00','12:30:00','IT-Lab1',3,2024,TRUE,'2024-07-01'),
(14,9,4,'Thursday','11:30:00','12:30:00','IT-Lab1',3,2024,TRUE,'2024-07-01'),
(15,10,4,'Friday','10:15:00','11:15:00','IT-203',3,2024,TRUE,'2024-07-01'),
(16,11,5,'Monday','14:00:00','15:00:00','ECE-101',1,2024,TRUE,'2024-07-01'),
(17,11,5,'Wednesday','14:00:00','15:00:00','ECE-101',1,2024,TRUE,'2024-07-01'),
(18,12,5,'Tuesday','14:00:00','15:00:00','ECE-Lab',1,2024,TRUE,'2024-07-01'),
(19,13,5,'Thursday','09:00:00','10:00:00','ECE-201',3,2024,TRUE,'2024-07-01'),
(20,14,6,'Monday','09:00:00','10:00:00','ME-101',1,2024,TRUE,'2024-07-01'),
(21,14,6,'Wednesday','10:15:00','11:15:00','ME-101',1,2024,TRUE,'2024-07-01'),
(22,15,6,'Tuesday','09:00:00','10:00:00','ME-Lab',1,2024,TRUE,'2024-07-01'),
(23,16,7,'Thursday','14:00:00','15:00:00','CE-101',1,2024,TRUE,'2024-07-01'),
(24,17,7,'Friday','14:00:00','15:30:00','CE-Lab',1,2024,TRUE,'2024-07-01'),
(25,18,8,'Monday','10:15:00','11:15:00','EE-101',1,2024,TRUE,'2024-07-01'),
(26,18,8,'Wednesday','10:15:00','11:15:00','EE-101',1,2024,TRUE,'2024-07-01'),
(27,19,9,'Tuesday','11:30:00','12:30:00','MATH-101',1,2024,TRUE,'2024-07-01'),
(28,19,9,'Thursday','11:30:00','12:30:00','MATH-101',1,2024,TRUE,'2024-07-01'),
(29,20,10,'Friday','09:00:00','10:00:00','PHY-101',1,2024,TRUE,'2024-07-01'),
(30,20,10,'Wednesday','14:00:00','15:00:00','PHY-Lab',1,2024,TRUE,'2024-07-01')
ON CONFLICT ("TimeSlotId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_academic."TimeSlots"', 'TimeSlotId'), (SELECT COALESCE(MAX("TimeSlotId"), 1) FROM cms_academic."TimeSlots"));

-- ==================== GRADES ====================
INSERT INTO cms_academic."Grades" ("GradeId","StudentId","CourseId","Marks","GradeLetter","Semester","Year","Remarks","CreatedAt") VALUES
(1,41,1,92,'A+',1,2023,'Excellent performance','2024-01-15'),
(2,42,1,78,'B+',1,2023,'Good effort','2024-01-15'),
(3,41,3,85,'A',3,2024,'Very good','2024-06-15'),
(4,42,4,72,'B',3,2024,NULL,'2024-06-15'),
(5,43,7,88,'A',1,2023,'Outstanding work','2024-01-15'),
(6,44,7,65,'C+',1,2023,NULL,'2024-01-15'),
(7,43,9,91,'A+',3,2024,'Top performer','2024-06-15'),
(8,44,10,74,'B',3,2024,NULL,'2024-06-15'),
(9,45,11,80,'A-',1,2023,'Good understanding','2024-01-15'),
(10,46,14,68,'C+',1,2023,NULL,'2024-01-15'),
(11,47,16,83,'A',1,2023,'Well done','2024-01-15'),
(12,48,18,76,'B+',1,2023,NULL,'2024-01-15'),
(13,49,19,95,'A+',1,2023,'Exceptional','2024-01-15'),
(14,50,20,70,'B-',1,2023,NULL,'2024-01-15')
ON CONFLICT ("GradeId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_academic."Grades"', 'GradeId'), (SELECT COALESCE(MAX("GradeId"), 1) FROM cms_academic."Grades"));

-- ==================== NOTICES ====================
INSERT INTO cms_academic."Notices" ("NoticeId","Title","Content","Category","TargetRole","CreatedByUserId","CreatedByName","IsActive","CreatedAt") VALUES
(1,'Welcome to Semester 2 — 2025','Dear students, welcome back! Classes begin on Jan 20.','Academic',NULL,1,'Admin',TRUE,'2025-01-10'),
(2,'Mid-Semester Exam Schedule','Mid-semester exams will be held from March 10-20. Please check the timetable.','Exam','Student',1,'Admin',TRUE,'2025-02-15'),
(3,'Annual Sports Day','Annual sports day on Feb 28. All are welcome to participate!','Event',NULL,1,'Admin',TRUE,'2025-02-01'),
(4,'Library Extended Hours','Library will remain open until 10 PM during exam season.','General',NULL,1,'Admin',TRUE,'2025-02-20'),
(5,'Faculty Meeting','Monthly faculty meeting on March 1 at 3 PM in Conference Hall.','General','Teacher',1,'Admin',TRUE,'2025-02-25'),
(6,'Scholarship Applications Open','Merit scholarship applications for 2025 are now open. Deadline: March 15.','Academic','Student',1,'Admin',TRUE,'2025-02-10'),
(7,'Hackathon 2025','24-hour hackathon on March 25. Register at the CS department.','Event','Student',1,'Admin',TRUE,'2025-03-01'),
(8,'New Lab Equipment','New IoT lab equipment has arrived. Training sessions start March 5.','Academic','Teacher',1,'Admin',TRUE,'2025-02-28'),
(9,'Holiday Notice — Holi','College will remain closed on March 14 for Holi.','General',NULL,1,'Admin',TRUE,'2025-03-05'),
(10,'Placement Drive — TCS','TCS campus placement drive on April 5. Eligibility: 7+ CGPA.','Academic','Student',1,'Admin',TRUE,'2025-03-10')
ON CONFLICT ("NoticeId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_academic."Notices"', 'NoticeId'), (SELECT COALESCE(MAX("NoticeId"), 1) FROM cms_academic."Notices"));

-- ==================== ATTENDANCE ====================
INSERT INTO cms_attendance."Attendances" ("AttendanceId","StudentId","CourseId","Date","IsPresent","Remarks") VALUES
(1,1,1,'2025-02-03',TRUE,NULL),(2,2,1,'2025-02-03',TRUE,NULL),(3,3,1,'2025-02-03',TRUE,NULL),
(4,4,1,'2025-02-03',FALSE,'Sick leave'),(5,5,1,'2025-02-03',TRUE,NULL),(6,6,1,'2025-02-03',TRUE,NULL),
(7,7,1,'2025-02-03',TRUE,NULL),(8,1,1,'2025-02-05',TRUE,NULL),(9,2,1,'2025-02-05',TRUE,NULL),
(10,3,1,'2025-02-05',FALSE,'Family emergency'),(11,4,1,'2025-02-05',TRUE,NULL),(12,5,1,'2025-02-05',TRUE,NULL),
(13,6,1,'2025-02-05',TRUE,NULL),(14,7,1,'2025-02-05',FALSE,NULL),(15,1,1,'2025-02-10',TRUE,NULL),
(16,2,1,'2025-02-10',FALSE,NULL),(17,3,1,'2025-02-10',TRUE,NULL),(18,4,1,'2025-02-10',TRUE,NULL),
(19,5,1,'2025-02-10',TRUE,NULL),(20,6,1,'2025-02-10',TRUE,NULL),(21,7,1,'2025-02-10',TRUE,NULL),
(22,1,1,'2025-02-12',TRUE,NULL),(23,2,1,'2025-02-12',TRUE,NULL),(24,3,1,'2025-02-12',TRUE,NULL),
(25,4,1,'2025-02-12',TRUE,NULL),(26,5,1,'2025-02-12',FALSE,'Late'),(27,6,1,'2025-02-12',TRUE,NULL),
(28,7,1,'2025-02-12',TRUE,NULL),(29,8,7,'2025-02-03',TRUE,NULL),(30,9,7,'2025-02-03',TRUE,NULL),
(31,10,7,'2025-02-03',FALSE,NULL),(32,11,7,'2025-02-03',TRUE,NULL),(33,12,7,'2025-02-03',TRUE,NULL),
(34,13,7,'2025-02-03',TRUE,NULL),(35,8,7,'2025-02-05',TRUE,NULL),(36,9,7,'2025-02-05',FALSE,NULL),
(37,10,7,'2025-02-05',TRUE,NULL),(38,11,7,'2025-02-05',TRUE,NULL),(39,12,7,'2025-02-05',TRUE,NULL),
(40,13,7,'2025-02-05',TRUE,NULL),(41,14,11,'2025-02-03',TRUE,NULL),(42,15,11,'2025-02-03',TRUE,NULL),
(43,16,11,'2025-02-03',FALSE,NULL),(44,17,11,'2025-02-03',TRUE,NULL),(45,18,11,'2025-02-03',TRUE,NULL),
(46,14,11,'2025-02-05',TRUE,NULL),(47,15,11,'2025-02-05',TRUE,NULL),(48,16,11,'2025-02-05',TRUE,NULL),
(49,17,11,'2025-02-05',FALSE,'Medical'),(50,18,11,'2025-02-05',TRUE,NULL)
ON CONFLICT ("AttendanceId") DO NOTHING;
SELECT setval(pg_get_serial_sequence('cms_attendance."Attendances"', 'AttendanceId'), (SELECT COALESCE(MAX("AttendanceId"), 1) FROM cms_attendance."Attendances"));

-- ============================================================
-- ✅ ALL DONE! Your CMS database is live on Supabase.
-- ============================================================
