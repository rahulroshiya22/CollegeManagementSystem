-- ============================================================
-- CMS AUTH DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys_auth
-- ============================================================
-- Run: psql -U postgres -d cms_auth -f 01_cms_auth.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Users" (
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

CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX IF NOT EXISTS "IX_Users_GoogleId" ON "Users" ("GoogleId");

CREATE TABLE IF NOT EXISTS "Teachers" (
    "TeacherId"       SERIAL PRIMARY KEY,
    "UserId"          INTEGER NOT NULL REFERENCES "Users"("UserId") ON DELETE CASCADE,
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

-- Role enum: 0=Student, 1=Teacher, 2=Admin
-- AuthProvider enum: 0=Local, 1=Google
