-- ============================================================
-- CMS FEES DATABASE - PostgreSQL
-- Original SQL Server DB: clgmansys4
-- ============================================================
-- Run: psql -U postgres -d cms_fees -f 05_cms_fees.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Fees" (
    "FeeId"        SERIAL PRIMARY KEY,
    "StudentId"    INTEGER NOT NULL,
    "Amount"       NUMERIC(18,2) NOT NULL DEFAULT 0,
    "Description"  TEXT NOT NULL DEFAULT '',
    "Status"       TEXT NOT NULL DEFAULT 'Pending',
    "DueDate"      TIMESTAMP NOT NULL,
    "PaidDate"     TIMESTAMP,
    "CreatedAt"    TIMESTAMP NOT NULL DEFAULT NOW()
);
