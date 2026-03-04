-- ============================================================
-- CMS COURSES DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys2
-- ============================================================
-- Run: psql -U postgres -d cms_courses -f 03_cms_courses.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Departments" (
    "DepartmentId"  SERIAL PRIMARY KEY,
    "Name"          TEXT NOT NULL DEFAULT '',
    "Code"          TEXT NOT NULL DEFAULT '',
    "IsActive"      BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Departments_Code" ON "Departments" ("Code");

CREATE TABLE IF NOT EXISTS "Courses" (
    "CourseId"       SERIAL PRIMARY KEY,
    "CourseCode"     VARCHAR(20) NOT NULL,
    "CourseName"     VARCHAR(200) NOT NULL,
    "Description"    TEXT NOT NULL DEFAULT '',
    "Credits"        INTEGER NOT NULL DEFAULT 0,
    "Semester"       INTEGER NOT NULL DEFAULT 0,
    "DepartmentId"   INTEGER NOT NULL REFERENCES "Departments"("DepartmentId"),
    "IsActive"       BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Courses_CourseCode" ON "Courses" ("CourseCode");

-- SEED: Departments
INSERT INTO "Departments" ("DepartmentId", "Name", "Code") VALUES
(1, 'Computer Science',       'CS'),
(2, 'Information Technology',  'IT'),
(3, 'Electronics',            'ECE'),
(4, 'Mechanical',             'ME'),
(5, 'Civil',                  'CE')
ON CONFLICT ("DepartmentId") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"Departments"', 'DepartmentId'), (SELECT COALESCE(MAX("DepartmentId"), 1) FROM "Departments"));
