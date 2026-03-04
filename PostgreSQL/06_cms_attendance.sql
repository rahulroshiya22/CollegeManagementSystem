-- ============================================================
-- CMS ATTENDANCE DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys5
-- ============================================================
-- Run: psql -U postgres -d cms_attendance -f 06_cms_attendance.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Attendances" (
    "AttendanceId"  SERIAL PRIMARY KEY,
    "StudentId"     INTEGER NOT NULL,
    "CourseId"      INTEGER NOT NULL,
    "Date"          TIMESTAMP NOT NULL,
    "IsPresent"     BOOLEAN NOT NULL DEFAULT FALSE,
    "Remarks"       TEXT
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Attendances_Student_Course_Date"
    ON "Attendances" ("StudentId", "CourseId", "Date");
