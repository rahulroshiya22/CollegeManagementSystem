# ============================================================
# Export ALL Data from SQL Server → PostgreSQL-Compatible SQL
# ============================================================
# This script connects to your local SQL Server, reads ALL data
# from ALL 8 CMS databases, and generates PostgreSQL INSERT statements.
#
# USAGE:
#   .\export_sqlserver_to_postgresql.ps1
#
# OUTPUT:
#   PostgreSQL\data_export\  (one .sql file per database with INSERT statements)
#
# REQUIREMENTS:
#   - SqlServer PowerShell module: Install-Module -Name SqlServer
#   - Your local SQL Server must be running
# ============================================================

$ErrorActionPreference = "Stop"

# Try importing SqlServer module
try {
    Import-Module SqlServer -ErrorAction Stop
}
catch {
    Write-Host "Installing SqlServer PowerShell module..." -ForegroundColor Yellow
    Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
    Import-Module SqlServer
}

$ServerName = "RAHUL"
$OutputDir = Join-Path $PSScriptRoot "PostgreSQL\data_export"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Database → Tables mapping
$Databases = @{
    "clgmansys_auth" = @{
        OutputFile = "01_auth_data.sql"
        TargetDb   = "cms_auth"
        Tables     = @("Users", "Teachers")
    }
    "clgmansys1"     = @{
        OutputFile = "02_students_data.sql"
        TargetDb   = "cms_students"
        Tables     = @("Departments", "Students")
    }
    "clgmansys2"     = @{
        OutputFile = "03_courses_data.sql"
        TargetDb   = "cms_courses"
        Tables     = @("Departments", "Courses")
    }
    "clgmansys3"     = @{
        OutputFile = "04_enrollments_data.sql"
        TargetDb   = "cms_enrollments"
        Tables     = @("Enrollments")
    }
    "clgmansys4"     = @{
        OutputFile = "05_fees_data.sql"
        TargetDb   = "cms_fees"
        Tables     = @("Fees")
    }
    "clgmansys5"     = @{
        OutputFile = "06_attendance_data.sql"
        TargetDb   = "cms_attendance"
        Tables     = @("Attendances")
    }
    "CMS_AcademicDb" = @{
        OutputFile = "07_academic_data.sql"
        TargetDb   = "cms_academic"
        Tables     = @("TimeSlots", "Grades", "Notices", "Messages", "GroupAnnouncements", "Exams", "ExamQuestions", "ExamSubmissions", "ExamAnswers", "ExamResults")
    }
    "ChatDB"         = @{
        OutputFile = "08_chat_data.sql"
        TargetDb   = "cms_chat"
        Tables     = @("Conversations", "ChatMessages")
    }
}

function Convert-ValueToPostgres {
    param($Value, $TypeName)

    if ($null -eq $Value -or $Value -is [DBNull]) {
        return "NULL"
    }

    switch -Regex ($TypeName) {
        "Int|Short|Long|Byte|Decimal|Double|Single" {
            return $Value.ToString()
        }
        "Boolean" {
            return if ($Value) { "TRUE" } else { "FALSE" }
        }
        "DateTime" {
            return "'" + $Value.ToString("yyyy-MM-dd HH:mm:ss.fff") + "'"
        }
        "TimeSpan" {
            return "'" + $Value.ToString("hh\:mm\:ss") + "'"
        }
        default {
            # String - escape single quotes
            $escaped = $Value.ToString().Replace("'", "''")
            return "'" + $escaped + "'"
        }
    }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  SQL Server → PostgreSQL Data Export" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""

$totalRows = 0

foreach ($dbName in $Databases.Keys) {
    $dbConfig = $Databases[$dbName]
    $outputFile = Join-Path $OutputDir $dbConfig.OutputFile
    $targetDb = $dbConfig.TargetDb

    Write-Host "Processing database: $dbName → $targetDb" -ForegroundColor Green

    $content = @()
    $content += "-- ============================================================"
    $content += "-- DATA EXPORT: $dbName → $targetDb (PostgreSQL)"
    $content += "-- Exported on: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $content += "-- ============================================================"
    $content += ""

    # Check if database exists
    try {
        $testQuery = "SELECT 1"
        Invoke-Sqlcmd -ServerInstance $ServerName -Database $dbName -Query $testQuery -TrustServerCertificate -ErrorAction Stop | Out-Null
    }
    catch {
        Write-Host "  WARNING: Database '$dbName' not found or not accessible. Skipping..." -ForegroundColor Yellow
        $content += "-- WARNING: Database '$dbName' was not found on server '$ServerName'"
        $content += "-- No data exported."
        $content -join "`n" | Out-File -FilePath $outputFile -Encoding UTF8
        continue
    }

    foreach ($tableName in $dbConfig.Tables) {
        Write-Host "  Exporting table: $tableName" -ForegroundColor Gray

        try {
            # Get all data
            $query = "SELECT * FROM [$tableName]"
            $rows = @(Invoke-Sqlcmd -ServerInstance $ServerName -Database $dbName -Query $query -TrustServerCertificate -ErrorAction Stop)

            if ($rows.Count -eq 0) {
                $content += "-- Table '$tableName': No data"
                $content += ""
                Write-Host "    → 0 rows" -ForegroundColor DarkGray
                continue
            }

            # Get column names (excluding RowError, RowState etc. from DataRow)
            $columns = @()
            $columnTypes = @{}
            $metaQuery = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '$tableName' ORDER BY ORDINAL_POSITION"
            $metaRows = @(Invoke-Sqlcmd -ServerInstance $ServerName -Database $dbName -Query $metaQuery -TrustServerCertificate)
            foreach ($meta in $metaRows) {
                $columns += $meta.COLUMN_NAME
            }

            $content += "-- Table: $tableName ($($rows.Count) rows)"
            $quotedCols = $columns | ForEach-Object { "`"$_`"" }
            $colList = $quotedCols -join ", "

            foreach ($row in $rows) {
                $values = @()
                foreach ($col in $columns) {
                    $val = $row.$col
                    $typeName = if ($null -ne $val -and $val -isnot [DBNull]) { $val.GetType().Name } else { "String" }
                    $values += Convert-ValueToPostgres -Value $val -TypeName $typeName
                }
                $valList = $values -join ", "
                $content += "INSERT INTO `"$tableName`" ($colList) VALUES ($valList);"
            }

            # Reset sequence for SERIAL columns (first column is typically the PK)
            $pkCol = $columns[0]
            $content += "SELECT setval(pg_get_serial_sequence('`"$tableName`"', '$pkCol'), (SELECT COALESCE(MAX(`"$pkCol`"), 1) FROM `"$tableName`"));"
            $content += ""

            $totalRows += $rows.Count
            Write-Host "    → $($rows.Count) rows exported" -ForegroundColor DarkGreen

        }
        catch {
            $content += "-- ERROR exporting table '$tableName': $($_.Exception.Message)"
            $content += ""
            Write-Host "    → ERROR: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    $content -join "`n" | Out-File -FilePath $outputFile -Encoding UTF8
    Write-Host "  → Saved to: $($dbConfig.OutputFile)" -ForegroundColor DarkCyan
    Write-Host ""
}

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  EXPORT COMPLETE!" -ForegroundColor Green
Write-Host "  Total rows exported: $totalRows" -ForegroundColor Green
Write-Host "  Output directory: $OutputDir" -ForegroundColor Green
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "NEXT STEPS:" -ForegroundColor Yellow
Write-Host "  1. Create PostgreSQL databases using: PostgreSQL\00_create_databases.sql"
Write-Host "  2. Run schema files: PostgreSQL\01_cms_auth.sql through 08_cms_chat.sql"
Write-Host "  3. Import this data: PostgreSQL\data_export\*.sql"
Write-Host ""
