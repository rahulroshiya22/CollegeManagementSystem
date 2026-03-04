-- ============================================================
-- CMS STUDENTS DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys1
-- ============================================================
-- Run: psql -U postgres -d cms_students -f 02_cms_students.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Departments" (
    "DepartmentId"  SERIAL PRIMARY KEY,
    "Name"          VARCHAR(200) NOT NULL,
    "Code"          VARCHAR(10) NOT NULL,
    "Description"   TEXT,
    "IsActive"      BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Departments_Code" ON "Departments" ("Code");

CREATE TABLE IF NOT EXISTS "Students" (
    "StudentId"      SERIAL PRIMARY KEY,
    "FirstName"      VARCHAR(100) NOT NULL,
    "LastName"       VARCHAR(100) NOT NULL,
    "Email"          VARCHAR(200) NOT NULL,
    "Phone"          TEXT NOT NULL DEFAULT '',
    "RollNumber"     VARCHAR(50) NOT NULL,
    "DateOfBirth"    TIMESTAMP NOT NULL,
    "Gender"         TEXT NOT NULL DEFAULT '',
    "Address"        TEXT NOT NULL DEFAULT '',
    "DepartmentId"   INTEGER NOT NULL REFERENCES "Departments"("DepartmentId") ON DELETE RESTRICT,
    "AdmissionYear"  INTEGER NOT NULL DEFAULT 0,
    "Status"         TEXT NOT NULL DEFAULT 'Active',
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt"      TIMESTAMP
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_Email" ON "Students" ("Email");
CREATE UNIQUE INDEX IF NOT EXISTS "IX_Students_RollNumber" ON "Students" ("RollNumber");

-- SEED: Departments
INSERT INTO "Departments" ("DepartmentId", "Name", "Code", "Description") VALUES
(1, 'Computer Science',       'CS',  'Computer Science and Engineering'),
(2, 'Information Technology',  'IT',  'Information Technology'),
(3, 'Electronics',            'ECE', 'Electronics and Communication Engineering'),
(4, 'Mechanical',             'ME',  'Mechanical Engineering'),
(5, 'Civil',                  'CE',  'Civil Engineering')
ON CONFLICT ("DepartmentId") DO NOTHING;

SELECT setval(pg_get_serial_sequence('"Departments"', 'DepartmentId'), (SELECT COALESCE(MAX("DepartmentId"), 1) FROM "Departments"));
