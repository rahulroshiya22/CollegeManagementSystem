-- ============================================================
-- CREATE ALL CMS PostgreSQL DATABASES
-- ============================================================
-- Run this as a PostgreSQL superuser (postgres):
--   psql -U postgres -f 00_create_databases.sql
-- ============================================================

-- Drop databases if they exist (COMMENT OUT if you want to keep existing data)
-- DROP DATABASE IF EXISTS cms_auth;
-- DROP DATABASE IF EXISTS cms_students;
-- DROP DATABASE IF EXISTS cms_courses;
-- DROP DATABASE IF EXISTS cms_enrollments;
-- DROP DATABASE IF EXISTS cms_fees;
-- DROP DATABASE IF EXISTS cms_attendance;
-- DROP DATABASE IF EXISTS cms_academic;
-- DROP DATABASE IF EXISTS cms_chat;

-- Create all 8 databases
CREATE DATABASE cms_auth;
CREATE DATABASE cms_students;
CREATE DATABASE cms_courses;
CREATE DATABASE cms_enrollments;
CREATE DATABASE cms_fees;
CREATE DATABASE cms_attendance;
CREATE DATABASE cms_academic;
CREATE DATABASE cms_chat;

-- ============================================================
-- After creating databases, run the schema files:
--   psql -U postgres -d cms_auth        -f 01_cms_auth.sql
--   psql -U postgres -d cms_students    -f 02_cms_students.sql
--   psql -U postgres -d cms_courses     -f 03_cms_courses.sql
--   psql -U postgres -d cms_enrollments -f 04_cms_enrollments.sql
--   psql -U postgres -d cms_fees        -f 05_cms_fees.sql
--   psql -U postgres -d cms_attendance  -f 06_cms_attendance.sql
--   psql -U postgres -d cms_academic    -f 07_cms_academic.sql
--   psql -U postgres -d cms_chat        -f 08_cms_chat.sql
-- ============================================================
