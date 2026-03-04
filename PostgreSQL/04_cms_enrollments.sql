-- ============================================================
-- CMS ENROLLMENTS DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys3
-- ============================================================
-- Run: psql -U postgres -d cms_enrollments -f 04_cms_enrollments.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Enrollments" (
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
    ON "Enrollments" ("StudentId", "CourseId", "Year", "Semester");
