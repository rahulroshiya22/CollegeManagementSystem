-- ============================================================
-- CMS CHAT DATABASE - PostgreSQL
-- Original SQL Server DB: ChatDB
-- ============================================================
-- Run: psql -U postgres -d cms_chat -f 08_cms_chat.sql
-- ============================================================

CREATE TABLE IF NOT EXISTS "Conversations" (
    "Id"             SERIAL PRIMARY KEY,
    "StudentId"      INTEGER NOT NULL,
    "CreatedAt"      TIMESTAMP NOT NULL DEFAULT NOW(),
    "LastMessageAt"  TIMESTAMP NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS "ChatMessages" (
    "Id"              SERIAL PRIMARY KEY,
    "ConversationId"  INTEGER NOT NULL REFERENCES "Conversations"("Id") ON DELETE CASCADE,
    "Role"            VARCHAR(20) NOT NULL,
    "Message"         TEXT NOT NULL,
    "Timestamp"       TIMESTAMP NOT NULL DEFAULT NOW(),
    "ServiceCalled"   TEXT
);
